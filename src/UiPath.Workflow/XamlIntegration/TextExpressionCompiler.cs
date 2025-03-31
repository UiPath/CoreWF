// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualBasic.Activities;

namespace System.Activities.XamlIntegration;

public class TextExpressionCompiler
{
    private const string TypedDataContextName = "_TypedDataContext";
    private const string ExpressionGetString = "__Expr{0}Get";
    private const string ExpressionSetString = "__Expr{0}Set";
    private const string ExpressionStatementString = "__Expr{0}Statement";
    private const string ExpressionGetTreeString = "__Expr{0}GetTree";
    private const string GetValueTypeValuesString = "GetValueTypeValues";
    private const string SetValueTypeValuesString = "SetValueTypeValues";
    private const string ValueTypeAccessorString = "ValueType_";
    private const string ForReadOnly = "_ForReadOnly";
    private const string XamlIntegrationNamespace = "System.Activities.XamlIntegration";
    private const string RootActivityFieldName = "rootActivity";
    private const string DataContextActivitiesFieldName = "dataContextActivities";
    private const string ForImplementationName = "forImplementation";
    private const string CSharpLambdaString = "() => ";
    private const string VbLambdaString = "Function() ";
    private const string LocationsOffsetFieldName = "locationsOffset";
    private const string ExpectedLocationsCountFieldName = "expectedLocationsCount";

    private static CodeAttributeDeclaration s_generatedCodeAttribute;
    private static CodeAttributeDeclaration s_browsableCodeAttribute;
    private static CodeAttributeDeclaration s_editorBrowsableCodeAttribute;

    private readonly Stack<CompiledDataContextDescriptor> _compiledDataContexts;
    private readonly List<CompiledExpressionDescriptor> _expressionDescriptors;
    private readonly Dictionary<int, IList<string>> _expressionIdToLocationReferences = new();

    private readonly string _fileName = null;

    // Dictionary of namespace name => [Line#]
    private readonly Dictionary<string, int> _lineNumbersForNSes;
    private readonly Dictionary<string, int> _lineNumbersForNSesForImpl;

    private readonly TextExpressionCompilerSettings _settings;

    private string _activityFullName;
    private CodeTypeDeclaration _classDeclaration;

    private CodeNamespace _codeNamespace;
    private CodeCompileUnit _compileUnit;
    private bool _generateSource;
    private bool? _isCs;
    private bool? _isVb;
    private int _nextContextId;

    public TextExpressionCompiler(TextExpressionCompilerSettings settings)
    {
        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        if (settings.Activity == null)
        {
            throw FxTrace.Exception.Argument(nameof(settings), SR.TextExpressionCompilerActivityRequired);
        }

        if (settings.ActivityName == null)
        {
            throw FxTrace.Exception.Argument(nameof(settings), SR.TextExpressionCompilerActivityNameRequired);
        }

        if (settings.Language == null)
        {
            throw FxTrace.Exception.Argument(nameof(settings), SR.TextExpressionCompilerLanguageRequired);
        }

        _expressionDescriptors = new List<CompiledExpressionDescriptor>();
        _compiledDataContexts = new Stack<CompiledDataContextDescriptor>();
        _nextContextId = 0;

        _settings = settings;

        _activityFullName = GetActivityFullName(settings);

        _generateSource = _settings.AlwaysGenerateSource;

        _lineNumbersForNSes = new Dictionary<string, int>();
        _lineNumbersForNSesForImpl = new Dictionary<string, int>();
    }

    private bool IsCs
    {
        get
        {
            _isCs ??= TextExpression.LanguagesAreEqual(_settings.Language, "C#");
            return _isCs.Value;
        }
    }

    private bool IsVb
    {
        get
        {
            _isVb ??= TextExpression.LanguagesAreEqual(_settings.Language, "VB");
            return _isVb.Value;
        }
    }

    private bool InVariableScopeArgument { get; set; }

    private static CodeAttributeDeclaration GeneratedCodeAttribute
    {
        get
        {
            if (s_generatedCodeAttribute == null)
            {
                var currentAssemblyName = new AssemblyName(Assembly.GetExecutingAssembly().FullName!);
                s_generatedCodeAttribute = new CodeAttributeDeclaration(
                    new CodeTypeReference(typeof(GeneratedCodeAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(currentAssemblyName.Name)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(currentAssemblyName.Version!.ToString())));
            }

            return s_generatedCodeAttribute;
        }
    }

    private static CodeAttributeDeclaration BrowsableCodeAttribute =>
        s_browsableCodeAttribute ??= new CodeAttributeDeclaration(
            new CodeTypeReference(typeof(BrowsableAttribute)),
            new CodeAttributeArgument(new CodePrimitiveExpression(false)));

    private static CodeAttributeDeclaration EditorBrowsableCodeAttribute =>
        s_editorBrowsableCodeAttribute ??= new CodeAttributeDeclaration(
            new CodeTypeReference(typeof(EditorBrowsableAttribute)),
            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(
                    new CodeTypeReference(typeof(EditorBrowsableState))), "Never")));
    
    public bool GenerateSource(TextWriter textWriter)
    {
        if (textWriter == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(textWriter));
        }

        Parse();

        if (_generateSource)
        {
            WriteCode(textWriter);
            return true;
        }

        return false;
    }

    public TextExpressionCompilerResults Compile()
    {
        Parse();

        if (_generateSource)
        {
            return CompileInMemory();
        }

        return new TextExpressionCompilerResults();
    }

    private void Parse()
    {
        if (!_settings.Activity.IsMetadataCached)
        {
            IList<ValidationError> validationErrors = null;
            var environment = new ActivityLocationReferenceEnvironment {CompileExpressions = true};
            try
            {
                ActivityUtilities.CacheRootMetadata(_settings.Activity, environment,
                    ProcessActivityTreeOptions.FullCachingOptions, null, ref validationErrors);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                throw FxTrace.Exception.AsError(new InvalidOperationException(
                    SR.CompiledExpressionsCacheMetadataException(_settings.Activity.GetType().AssemblyQualifiedName,
                        e.ToString())));
            }
        }

        _compileUnit = new CodeCompileUnit();
        _codeNamespace = GenerateCodeNamespace();
        _classDeclaration = GenerateClass();

        _codeNamespace.Types.Add(_classDeclaration);
        _compileUnit.Namespaces.Add(_codeNamespace);

        //
        // Generate data contexts with properties and expression methods
        // Use the shared, public tree walk for expressions routine for consistency.       
        var visitor = new ExpressionCompilerActivityVisitor(this)
        {
            NextExpressionId = 0
        };

        try
        {
            visitor.Visit(_settings.Activity, _settings.ForImplementation);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            //
            // Note that unlike the above where the exception from CacheMetadata is always going to be from the user's code 
            // an exception here is more likely to be from our code and unexpected.  However it could be from user code in some cases.
            // Output a message that attempts to normalize this and presents enough info to the user to determine if they can take action.                
            throw FxTrace.Exception.AsError(new InvalidOperationException(
                SR.CompiledExpressionsActivityException(e.GetType().FullName,
                    _settings.Activity.GetType().AssemblyQualifiedName, e.ToString())));
        }

        if (_generateSource)
        {
            GenerateInvokeExpressionMethod(true);
            GenerateInvokeExpressionMethod(false);

            GenerateCanExecuteMethod();

            GenerateGetRequiredLocationsMethod();

            GenerateGetExpressionTreeForExpressionMethod();
        }
    }

    private void OnRootActivity()
    {
        //
        // Always generate a CDC for the root
        // This will contain expressions for the default value of the root arguments
        // These expressions cannot see other root arguments or variables so they need 
        // to be at the very root, before we add any properties
        PushDataContextDescriptor();
    }

    private void OnAfterRootActivity()
    {
        //
        // First pop the root arguments descriptor pushed in OnAfterRootArguments
        PopDataContextDescriptor();
        //
        // If we are walking the implementation there will be a second root context descriptor
        // that holds the member declarations for root arguments.   
        // This isn't generated when walking the public surface
        if (_settings.ForImplementation)
        {
            PopDataContextDescriptor();
        }
    }

    private void OnAfterRootArguments(Activity activity)
    {
        //
        // Generate the properties for root arguments in a context below the context
        // that contains the default expressions for the root arguments
        var contextDescriptor = PushDataContextDescriptor();
        if (activity.RuntimeArguments is {Count: > 0})
            //
            // Walk the arguments
        {
            foreach (var runtimeArgument in activity.RuntimeArguments)
            {
                if (runtimeArgument.IsBound)
                {
                    AddMember(runtimeArgument.Name, runtimeArgument.Type, contextDescriptor);
                }
            }
        }
    }

    private void OnActivityDelegateScope() => PushDataContextDescriptor();

    private void OnDelegateArgument(RuntimeDelegateArgument delegateArgument) =>
        AddMember(delegateArgument.BoundArgument.Name, delegateArgument.BoundArgument.Type,
            _compiledDataContexts.Peek());

    private void OnAfterActivityDelegateScope() => PopDataContextDescriptor();

    private void OnVariableScope(Activity activity)
    {
        var contextDescriptor = PushDataContextDescriptor();
        //
        // Generate the variable accessors
        foreach (var v in activity.RuntimeVariables)
        {
            AddMember(v.Name, v.Type, VariableModifiersHelper.IsReadOnly(v.Modifiers), contextDescriptor);
        }
    }

    private void OnRootImplementationScope(Activity activity,
        out CompiledDataContextDescriptor rootArgumentAccessorContext)
    {
        Fx.Assert(_compiledDataContexts.Count == 2,
            "The stack of data contexts should contain the root argument default expression and accessor contexts");

        rootArgumentAccessorContext = _compiledDataContexts.Pop();

        if (activity.RuntimeVariables is {Count: > 0})
        {
            OnVariableScope(activity);
        }
    }

    private void OnAfterRootImplementationScope(Activity activity,
        CompiledDataContextDescriptor rootArgumentAccessorContext)
    {
        if (activity.RuntimeVariables is {Count: > 0})
        {
            OnAfterVariableScope();
        }

        _compiledDataContexts.Push(rootArgumentAccessorContext);
    }

    private void AddMember(string name, Type type, CompiledDataContextDescriptor contextDescriptor)
        => AddMember(name, type, isReadonly: false, contextDescriptor);

    private void AddMember(string name, Type type, bool isReadonly, CompiledDataContextDescriptor contextDescriptor)
    {
        if (IsValidTextIdentifierName(name))
        {
            //
            // These checks will be invariantlowercase if the language is VB
            if (contextDescriptor.Fields.ContainsKey(name) || contextDescriptor.Properties.ContainsKey(name))
            {
                if (!contextDescriptor.Duplicates.Contains(name))
                {
                    contextDescriptor.Duplicates.Add(name.ToUpperInvariant());
                }
            }
            else
            {
                var memberData = new MemberData
                {
                    Type = type,
                    Name = name,
                    Index = contextDescriptor.NextMemberIndex,
                    IsReadonly = isReadonly
                };

                if (type.IsValueType)
                {
                    contextDescriptor.Fields.Add(name, memberData);
                }
                else
                {
                    contextDescriptor.Properties.Add(name, memberData);
                }
            }
        }

        //
        // Regardless of whether or not this member name is an invalid, duplicate, or valid identifier
        // always increment the member count so that the indexes we generate always match
        // the list that the runtime gives to the ITextExpression
        // The exception here is if the name is null
        if (name != null)
        {
            contextDescriptor.NextMemberIndex++;
        }
    }

    private void GenerateMembers(CompiledDataContextDescriptor descriptor)
    {
        foreach (var property in descriptor.Properties)
        {
            GenerateProperty(property.Value, descriptor);
        }

        if (descriptor.Fields.Count > 0)
        {
            foreach (var field in descriptor.Fields)
            {
                GenerateField(field.Value, descriptor);
            }

            var getValueTypeValuesMethod = GenerateGetValueTypeValues(descriptor);

            descriptor.CodeTypeDeclaration.Members.Add(getValueTypeValuesMethod);
            descriptor.CodeTypeDeclaration.Members.Add(GenerateSetValueTypeValues(descriptor));

            descriptor.CodeTypeDeclarationForReadOnly.Members.Add(getValueTypeValuesMethod);
        }

        if (descriptor.Duplicates.Count > 0 && IsVb)
        {
            foreach (var duplicate in descriptor.Duplicates)
            {
                AddPropertyForDuplicates(duplicate, descriptor);
            }
        }
    }

    private void GenerateField(MemberData memberData, CompiledDataContextDescriptor contextDescriptor)
    {
        if (contextDescriptor.Duplicates.Contains(memberData.Name))
        {
            return;
        }

        var accessorField = new CodeMemberField
        {
            Attributes = MemberAttributes.Family | MemberAttributes.Final,
            Name = memberData.Name,
            Type = new CodeTypeReference(memberData.Type)
        };

        if (IsRedefinition(memberData.Name))
        {
            accessorField.Attributes |= MemberAttributes.New;
        }

        contextDescriptor.CodeTypeDeclaration.Members.Add(accessorField);

        contextDescriptor.CodeTypeDeclarationForReadOnly.Members.Add(accessorField);
    }

    private void GenerateProperty(MemberData memberData, CompiledDataContextDescriptor contextDescriptor)
    {
        if (contextDescriptor.Duplicates.Contains(memberData.Name))
        {
            return;
        }

        var isRedefinition = IsRedefinition(memberData.Name);

        var accessorProperty = GenerateCodeMemberProperty(memberData, isRedefinition);

        //
        // Generate a get accessor that looks like this:
        // return (Foo) this.GetVariableValue(contextId, locationIndexId)
        var getterStatement = new CodeMethodReturnStatement(
            new CodeCastExpression(memberData.Type, new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeThisReferenceExpression(),
                    "GetVariableValue"),
                new CodeBinaryOperatorExpression(
                    new CodePrimitiveExpression(memberData.Index),
                    CodeBinaryOperatorType.Add,
                    new CodeVariableReferenceExpression("locationsOffset")))));

        accessorProperty.GetStatements.Add(getterStatement);

        // Generate a set accessor that looks something like this:
        // this.SetVariableValue(contextId, locationIndexId, value)
        accessorProperty.SetStatements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                "SetVariableValue"),
            new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(memberData.Index),
                CodeBinaryOperatorType.Add,
                new CodeVariableReferenceExpression("locationsOffset")),
            new CodePropertySetValueReferenceExpression()));

        contextDescriptor.CodeTypeDeclaration.Members.Add(accessorProperty);

        //
        // Create another property for the read only class.
        // This will only have a getter so we can't just re-use the property from above
        var accessorPropertyForReadOnly = GenerateCodeMemberProperty(memberData, isRedefinition);
        //
        // OK to share the getter statement from above
        accessorPropertyForReadOnly.GetStatements.Add(getterStatement);

        contextDescriptor.CodeTypeDeclarationForReadOnly.Members.Add(accessorPropertyForReadOnly);
    }

    private static CodeMemberProperty GenerateCodeMemberProperty(MemberData memberData, bool isRedefinition)
    {
        var accessorProperty = new CodeMemberProperty
        {
            Attributes = MemberAttributes.Family | MemberAttributes.Final,
            Name = memberData.Name,
            Type = new CodeTypeReference(memberData.Type)
        };

        if (isRedefinition)
        {
            accessorProperty.Attributes |= MemberAttributes.New;
        }

        return accessorProperty;
    }

    private static void AddPropertyForDuplicates(string name, CompiledDataContextDescriptor contextDescriptor)
    {
        var accessorProperty = new CodeMemberProperty
        {
            Attributes = MemberAttributes.Family | MemberAttributes.Final,
            Name = name,
            Type = new CodeTypeReference(typeof(object))
        };

        var exception = new CodeThrowExceptionStatement(
            new CodeObjectCreateExpression(typeof(InvalidOperationException),
                new CodePrimitiveExpression(SR.CompiledExpressionsDuplicateName(name))));

        accessorProperty.GetStatements.Add(exception);
        accessorProperty.SetStatements.Add(exception);

        contextDescriptor.CodeTypeDeclaration.Members.Add(accessorProperty);

        //
        // Create another property for the read only class.
        // This will only have a getter so we can't just re-use the property from above
        var accessorPropertyForReadOnly = new CodeMemberProperty
        {
            Attributes = MemberAttributes.Family | MemberAttributes.Final,
            Name = name,
            Type = new CodeTypeReference(typeof(object))
        };
        //
        // OK to share the exception from above
        accessorPropertyForReadOnly.GetStatements.Add(exception);

        contextDescriptor.CodeTypeDeclarationForReadOnly.Members.Add(accessorPropertyForReadOnly);
    }

    private bool IsValidTextIdentifierName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _settings.LogSourceGenerationMessage?.Invoke(SR.CompiledExpressionsIgnoringUnnamedVariable);

            return false;
        }

        if (!CodeDomProvider.CreateProvider(_settings.Language).IsValidIdentifier(name))
        {
            _settings.LogSourceGenerationMessage?.Invoke(SR.CompiledExpressionsIgnoringInvalidIdentifierVariable(name));

            return false;
        }

        return true;
    }

    private bool IsRedefinition(string variableName)
    {
        if (_compiledDataContexts == null)
        {
            return false;
        }

        foreach (var contextDescriptor in _compiledDataContexts)
        {
            if (contextDescriptor.Fields.Any(field => NamesMatch(variableName, field.Key)))
            {
                return true;
            }

            if (contextDescriptor.Properties.Any(property => NamesMatch(variableName, property.Key)))
            {
                return true;
            }
        }

        return false;
    }

    private bool NamesMatch(string toCheck, string current)
    {
        if (IsVb && string.Compare(toCheck, current, true, CultureInfo.CurrentCulture) == 0)
        {
            return true;
        }

        if (!IsVb && toCheck == current)
        {
            return true;
        }

        return false;
    }

    private void OnAfterVariableScope() => PopDataContextDescriptor();

    private void OnITextExpressionFound(Activity activity, ExpressionCompilerActivityVisitor visitor)
    {
        CompiledDataContextDescriptor contextDescriptor = null;
        var currentContextDescriptor = _compiledDataContexts.Peek();

        if (InVariableScopeArgument)
        {
            //
            // Temporarily popping the stack so don't use PopDataContextDescriptor
            // because that is for when the descriptor is done being built
            _compiledDataContexts.Pop();
            contextDescriptor = PushDataContextDescriptor();
        }
        else
        {
            contextDescriptor = currentContextDescriptor;
        }

        if (TryGenerateExpressionCode(activity, contextDescriptor, visitor.NextExpressionId, _settings.Language))
        {
            _expressionIdToLocationReferences.Add(visitor.NextExpressionId, FindLocationReferences(activity));
            visitor.NextExpressionId++;
            _generateSource = true;
        }

        if (InVariableScopeArgument)
        {
            PopDataContextDescriptor();
            _compiledDataContexts.Push(currentContextDescriptor);
        }
    }

    private IList<string> FindLocationReferences(Activity activity)
    {
        ActivityWithResult boundExpression;
        LocationReference locationReference;
        var requiredLocationReferences = new List<string>();

        foreach (var runtimeArgument in activity.RuntimeArguments)
        {
            boundExpression = runtimeArgument.BoundArgument.Expression;

            if (boundExpression is ILocationReferenceWrapper wrapper)
            {
                locationReference = wrapper.LocationReference;

                if (locationReference != null)
                {
                    requiredLocationReferences.Add(locationReference.Name);
                }
            }
        }

        return requiredLocationReferences;
    }

    private CodeTypeDeclaration GenerateClass()
    {
        var classDeclaration = new CodeTypeDeclaration(_settings.ActivityName);
        classDeclaration.BaseTypes.Add(new CodeTypeReference(typeof(ICompiledExpressionRoot)));
        classDeclaration.IsPartial = _settings.GenerateAsPartialClass;

        var compiledRootField = new CodeMemberField(new CodeTypeReference(typeof(Activity)), RootActivityFieldName);
        classDeclaration.Members.Add(compiledRootField);

        var languageProperty = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Final | MemberAttributes.Public,
            Name = "GetLanguage",
            ReturnType = new CodeTypeReference(typeof(string))
        };
        languageProperty.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(_settings.Language)));
        languageProperty.ImplementationTypes.Add(new CodeTypeReference(typeof(ICompiledExpressionRoot)));
        languageProperty.CustomAttributes.Add(GeneratedCodeAttribute);
        languageProperty.CustomAttributes.Add(BrowsableCodeAttribute);
        languageProperty.CustomAttributes.Add(EditorBrowsableCodeAttribute);

        classDeclaration.Members.Add(languageProperty);

        var dataContextActivitiesField = new CodeMemberField
        {
            Attributes = MemberAttributes.Private,
            Name = DataContextActivitiesFieldName,
            Type = new CodeTypeReference(typeof(object))
        };

        classDeclaration.Members.Add(dataContextActivitiesField);

        var forImplementationField = new CodeMemberField
        {
            Attributes = MemberAttributes.Private,
            Name = ForImplementationName,
            Type = new CodeTypeReference(typeof(bool)),
            InitExpression = new CodePrimitiveExpression(_settings.ForImplementation)
        };

        classDeclaration.Members.Add(forImplementationField);

        if (!_settings.GenerateAsPartialClass)
        {
            classDeclaration.Members.Add(GenerateCompiledExpressionRootConstructor());
        }

        return classDeclaration;
    }

    private CodeConstructor GenerateCompiledExpressionRootConstructor()
    {
        var constructor = new CodeConstructor
        {
            Attributes = MemberAttributes.Public
        };

        constructor.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(Activity)),
                RootActivityFieldName));

        var nullArgumentExpression = new CodeBinaryOperatorExpression(
            new CodeVariableReferenceExpression(RootActivityFieldName),
            CodeBinaryOperatorType.IdentityEquality,
            new CodePrimitiveExpression(null));

        var nullArgumentCondition = new CodeConditionStatement(
            nullArgumentExpression,
            new CodeThrowExceptionStatement(
                new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(ArgumentNullException)),
                    new CodePrimitiveExpression(RootActivityFieldName))));

        constructor.Statements.Add(nullArgumentCondition);

        constructor.Statements.Add(
            new CodeAssignStatement(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    RootActivityFieldName),
                new CodeVariableReferenceExpression(RootActivityFieldName)));

        return constructor;
    }

    private Dictionary<string, int> GetCacheIndices()
    {
        var contexts = new Dictionary<string, int>();
        var currentIndex = 0;

        foreach (var descriptor in _expressionDescriptors)
        {
            var name = descriptor.TypeName;
            if (!contexts.ContainsKey(name))
            {
                contexts.Add(name, currentIndex++);
            }
        }

        return contexts;
    }

    private void GenerateGetRequiredLocationsMethod()
    {
        var getLocationsMethod = new CodeMemberMethod
        {
            Name = "GetRequiredLocations",
            Attributes = MemberAttributes.Final | MemberAttributes.Public
        };
        getLocationsMethod.CustomAttributes.Add(GeneratedCodeAttribute);
        getLocationsMethod.CustomAttributes.Add(BrowsableCodeAttribute);
        getLocationsMethod.CustomAttributes.Add(EditorBrowsableCodeAttribute);
        getLocationsMethod.ImplementationTypes.Add(new CodeTypeReference(typeof(ICompiledExpressionRoot)));

        getLocationsMethod.ReturnType = new CodeTypeReference(typeof(IList<string>));

        getLocationsMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "expressionId"));

        if (IsVb)
        {
            GenerateRequiredLocationsBody(getLocationsMethod);
        }
        else
        {
            GenerateEmptyRequiredLocationsBody(getLocationsMethod);
        }

        _classDeclaration.Members.Add(getLocationsMethod);
    }

    private static void GenerateEmptyRequiredLocationsBody(CodeMemberMethod getLocationsMethod) =>
        getLocationsMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));

    private void GenerateRequiredLocationsBody(CodeMemberMethod getLocationsMethod)
    {
        var returnLocationsVar = new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(List<string>)),
            "returnLocations",
            new CodeObjectCreateExpression(new CodeTypeReference(typeof(List<string>))));

        getLocationsMethod.Statements.Add(returnLocationsVar);
        foreach (var descriptor in _expressionDescriptors)
        {
            if (!_expressionIdToLocationReferences.TryGetValue(descriptor.Id, out var requiredLocations))
            {
                return;
            }

            CodeStatement[] conditionStatements = null;
            conditionStatements = GetRequiredLocationsConditionStatements(requiredLocations);

            var idExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("expressionId"),
                CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(descriptor.Id));
            var idCondition = new CodeConditionStatement(idExpression, conditionStatements);

            getLocationsMethod.Statements.Add(idCondition);
        }

        getLocationsMethod.Statements.Add(
            new CodeMethodReturnStatement(new CodeVariableReferenceExpression("returnLocations")));
    }

    private static CodeStatement[] GetRequiredLocationsConditionStatements(IList<string> requiredLocations)
    {
        var statementCollection = new CodeStatementCollection();
        foreach (var locationName in requiredLocations)
        {
            var invokeValidateExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("returnLocations"), "Add"),
                new CodePrimitiveExpression(locationName));
            statementCollection.Add(invokeValidateExpression);
        }

        var returnStatements = new CodeStatement[statementCollection.Count];
        statementCollection.CopyTo(returnStatements, 0);

        return returnStatements;
    }

    private void GenerateGetExpressionTreeForExpressionMethod()
    {
        var getExpressionTreeForExpressionMethod = new CodeMemberMethod
        {
            Name = "GetExpressionTreeForExpression",
            Attributes = MemberAttributes.Final | MemberAttributes.Public,
            ReturnType = new CodeTypeReference(typeof(Expression))
        };
        getExpressionTreeForExpressionMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "expressionId"));
        getExpressionTreeForExpressionMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IList<LocationReference>)),
                "locationReferences"));
        getExpressionTreeForExpressionMethod.ImplementationTypes.Add(
            new CodeTypeReference(typeof(ICompiledExpressionRoot)));

        // Mark this type as tool generated code
        getExpressionTreeForExpressionMethod.CustomAttributes.Add(GeneratedCodeAttribute);

        // Mark it as Browsable(false) 
        // Note that this does not prevent intellisense within a single project, just at the metadata level
        getExpressionTreeForExpressionMethod.CustomAttributes.Add(BrowsableCodeAttribute);

        // Mark it as EditorBrowsable(EditorBrowsableState.Never)
        // Note that this does not prevent intellisense within a single project, just at the metadata level
        getExpressionTreeForExpressionMethod.CustomAttributes.Add(EditorBrowsableCodeAttribute);

        foreach (var descriptor in _expressionDescriptors)
        {
            var conditionStatement = new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeObjectCreateExpression(new CodeTypeReference(descriptor.TypeName),
                            new CodeVariableReferenceExpression("locationReferences")),
                        descriptor.GetExpressionTreeMethodName)));

            var idExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("expressionId"),
                CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(descriptor.Id));
            var idCondition = new CodeConditionStatement(idExpression, conditionStatement);

            getExpressionTreeForExpressionMethod.Statements.Add(idCondition);
        }

        getExpressionTreeForExpressionMethod.Statements.Add(new CodeMethodReturnStatement(
            new CodePrimitiveExpression(null)));

        _classDeclaration.Members.Add(getExpressionTreeForExpressionMethod);
    }

    private void GenerateInvokeExpressionMethod(bool withLocationReferences)
    {
        var invokeExpressionMethod = new CodeMemberMethod
        {
            Name = "InvokeExpression",
            Attributes = MemberAttributes.Final | MemberAttributes.Public
        };
        invokeExpressionMethod.CustomAttributes.Add(GeneratedCodeAttribute);
        invokeExpressionMethod.CustomAttributes.Add(BrowsableCodeAttribute);
        invokeExpressionMethod.CustomAttributes.Add(EditorBrowsableCodeAttribute);
        invokeExpressionMethod.ImplementationTypes.Add(new CodeTypeReference(typeof(ICompiledExpressionRoot)));

        invokeExpressionMethod.ReturnType = new CodeTypeReference(typeof(object));

        invokeExpressionMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "expressionId"));

        if (withLocationReferences)
        {
            invokeExpressionMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IList<LocationReference>)),
                    "locations"));
            invokeExpressionMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(ActivityContext)),
                    "activityContext"));
        }
        else
        {
            invokeExpressionMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IList<Location>)), "locations"));
        }

        if (_settings.GenerateAsPartialClass)
        {
            invokeExpressionMethod.Statements.Add(GenerateInitializeDataContextActivity());
        }

        if (withLocationReferences && _expressionDescriptors is {Count: > 0})
        {
            //
            // We only generate the helper method on the root data context/context 0
            // No need to have it on all contexts.  This is just a slight of hand
            // so that we don't need to make GetDataContextActivities public on CompiledDataContext.
            invokeExpressionMethod.Statements.Add(GenerateDataContextActivitiesCheck(_expressionDescriptors[0]));
        }

        var cacheIndices = GetCacheIndices();

        foreach (var descriptor in _expressionDescriptors)
        {
            //
            // if ((expressionId == [descriptor.Id]))
            // {
            //   if (!CheckExpressionText(expressionId, activityContext)
            //   {
            //     throw new Exception();
            //   }
            //   System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Workflow1_TypedDataContext1_ForReadOnly.GetCompiledDataContextCacheHelper(this, activityContext, 1);
            //   if ((cachedCompiledDataContext[0] == null))
            //   {
            //     cachedCompiledDataContext[0] = new Workflow1_TypedDataContext1_ForReadOnly(locations, activityContext);
            //   }
            //   Workflow1_TypedDataContext1_ForReadOnly valDataContext0 = ((Workflow1_TypedDataContext1_ForReadOnly)(cachedCompiledDataContext[0]));
            //   return valDataContext0.ValueType___Expr0Get();
            // }
            //
            CodeStatement[] conditionStatements = null;
            if (descriptor.IsReference)
            {
                conditionStatements =
                    GenerateReferenceExpressionInvocation(descriptor, withLocationReferences, cacheIndices);
            }
            else if (descriptor.IsValue)
            {
                conditionStatements =
                    GenerateValueExpressionInvocation(descriptor, withLocationReferences, cacheIndices);
            }
            else if (descriptor.IsStatement)
            {
                conditionStatements = GenerateStatementInvocation(descriptor, withLocationReferences, cacheIndices);
            }

            var idExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("expressionId"),
                CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(descriptor.Id));
            var idCondition = new CodeConditionStatement(idExpression, conditionStatements);

            invokeExpressionMethod.Statements.Add(idCondition);
        }

        invokeExpressionMethod.Statements.Add(new CodeMethodReturnStatement(
            new CodePrimitiveExpression(null)));

        _classDeclaration.Members.Add(invokeExpressionMethod);
    }

    private static CodeConditionStatement GenerateDataContextActivitiesCheck(CompiledExpressionDescriptor descriptor)
    {
        var dataContextActivitiesNullExpression = new CodeBinaryOperatorExpression(
            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), DataContextActivitiesFieldName),
            CodeBinaryOperatorType.IdentityEquality,
            new CodePrimitiveExpression(null));

        var dataContextActivitiesNullStatement = new CodeConditionStatement(
            dataContextActivitiesNullExpression,
            new CodeAssignStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), DataContextActivitiesFieldName),
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(descriptor.TypeName)),
                        "GetDataContextActivitiesHelper"),
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        RootActivityFieldName),
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        ForImplementationName))));

        return dataContextActivitiesNullStatement;
    }


    private static CodeStatement GenerateInitializeDataContextActivity()
    {
        //
        // if (this.rootActivity == null)
        // {
        //   this.rootActivity == this;
        // }
        var dataContextActivityExpression = new CodeBinaryOperatorExpression(
            new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(),
                RootActivityFieldName),
            CodeBinaryOperatorType.IdentityEquality,
            new CodePrimitiveExpression(null));

        var dataContextActivityCheck = new CodeConditionStatement(
            dataContextActivityExpression,
            new CodeAssignStatement(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    RootActivityFieldName),
                new CodeThisReferenceExpression()));

        return dataContextActivityCheck;
    }

    private void GenerateGetDataContextVariable(CompiledExpressionDescriptor descriptor,
        CodeVariableDeclarationStatement dataContextVariable, CodeStatementCollection statements,
        bool withLocationReferences, IReadOnlyDictionary<string, int> cacheIndices)
    {
        var dataContext = GenerateDataContextCreateExpression(descriptor.TypeName, withLocationReferences);

        if (withLocationReferences)
        {
            //
            // System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = CompiledExpressions_TypedDataContext2.GetCompiledDataContextCacheHelper(this, activityContext, 2);
            // if ((cachedCompiledDataContext[1] == null))
            // {
            //   if (!CompiledExpressions_TypedDataContext2.Validate(locations, activityContext))
            //   {
            //     return false;
            //   }
            //   cachedCompiledDataContext[1] = new CompiledExpressions_TypedDataContext2(locations, activityContext);
            // }
            //
            var cachedCompiledDataContextArray = new CodeVariableDeclarationStatement(
                typeof(CompiledDataContext[]),
                "cachedCompiledDataContext",
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(descriptor.TypeName),
                        "GetCompiledDataContextCacheHelper"),
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        DataContextActivitiesFieldName),
                    new CodeVariableReferenceExpression("activityContext"),
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        RootActivityFieldName),
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        ForImplementationName),
                    new CodePrimitiveExpression(cacheIndices.Count)));

            var compiledDataContextIndexer = new CodeIndexerExpression(
                new CodeVariableReferenceExpression("cachedCompiledDataContext"),
                new CodePrimitiveExpression(cacheIndices[descriptor.TypeName]));

            //
            // if (cachedCompiledDataContext[index] == null)
            // {
            //     cachedCompiledDataContext[index] = new TCDC(locations, activityContext);
            // }
            //

            var nullCacheItemExpression = new CodeBinaryOperatorExpression(
                compiledDataContextIndexer,
                CodeBinaryOperatorType.IdentityEquality,
                new CodePrimitiveExpression(null));

            var cacheIndexInitializer = new CodeAssignStatement(
                compiledDataContextIndexer,
                dataContext);

            var conditionStatement = new CodeConditionStatement(
                nullCacheItemExpression,
                cacheIndexInitializer);

            //
            // [compiledDataContextVariable] = cachedCompiledDataContext[index]
            //

            dataContextVariable.InitExpression =
                new CodeCastExpression(descriptor.TypeName, compiledDataContextIndexer);


            statements.Add(cachedCompiledDataContextArray);
            statements.Add(conditionStatement);
        }
        else
        {
            //
            // [compiledDataContextVariable] = new [compiledDataContextType](locations);
            //

            dataContextVariable.InitExpression = dataContext;
        }
    }

    private CodeStatement[] GenerateReferenceExpressionInvocation(CompiledExpressionDescriptor descriptor,
        bool withLocationReferences, IReadOnlyDictionary<string, int> cacheIndices)
    {
        var indexString = descriptor.Id.ToString(CultureInfo.InvariantCulture);
        var dataContextVariableName = "refDataContext" + indexString;

        var dataContextVariable = new CodeVariableDeclarationStatement(
            new CodeTypeReference(descriptor.TypeName), dataContextVariableName);

        var compiledDataContextStatements = new CodeStatementCollection();

        GenerateGetDataContextVariable(descriptor, dataContextVariable, compiledDataContextStatements,
            withLocationReferences, cacheIndices);
        compiledDataContextStatements.Add(dataContextVariable);

        CodeExpression getExpression = null;
        CodeExpression setExpression = null;

        if (IsVb)
        {
            getExpression = new CodeDelegateCreateExpression(
                new CodeTypeReference(descriptor.TypeName),
                new CodeVariableReferenceExpression(dataContextVariableName),
                descriptor.GetMethodName);
            setExpression = new CodeDelegateCreateExpression(
                new CodeTypeReference(descriptor.TypeName),
                new CodeVariableReferenceExpression(dataContextVariableName),
                descriptor.SetMethodName);
        }
        else
        {
            getExpression =
                new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dataContextVariableName),
                    descriptor.GetMethodName);
            setExpression =
                new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dataContextVariableName),
                    descriptor.SetMethodName);
        }

        var getLocationMethod = new CodeMethodReferenceExpression(
            new CodeVariableReferenceExpression(dataContextVariableName),
            "GetLocation", new CodeTypeReference(descriptor.ResultType));

        CodeExpression[] getLocationParameters = null;
        if (withLocationReferences)
        {
            getLocationParameters = new[]
            {
                getExpression,
                setExpression,
                new CodeVariableReferenceExpression("expressionId"),
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    RootActivityFieldName),
                new CodeVariableReferenceExpression("activityContext")
            };
        }
        else
        {
            getLocationParameters = new[]
            {
                getExpression,
                setExpression
            };
        }

        var getLocationExpression = new CodeMethodInvokeExpression(
            getLocationMethod,
            getLocationParameters);


        var returnStatement = new CodeMethodReturnStatement(getLocationExpression);

        compiledDataContextStatements.Add(returnStatement);

        var returnStatements = new CodeStatement[compiledDataContextStatements.Count];
        compiledDataContextStatements.CopyTo(returnStatements, 0);

        return returnStatements;
    }

    private CodeStatement[] GenerateValueExpressionInvocation(CompiledExpressionDescriptor descriptor,
        bool withLocationReferences, IReadOnlyDictionary<string, int> cacheIndices)
    {
        var compiledDataContextStatements = new CodeStatementCollection();

        var indexString = descriptor.Id.ToString(CultureInfo.InvariantCulture);
        var dataContextVariableName = "valDataContext" + indexString;

        var dataContextVariable = new CodeVariableDeclarationStatement(
            new CodeTypeReference(descriptor.TypeName), dataContextVariableName);

        GenerateGetDataContextVariable(descriptor, dataContextVariable, compiledDataContextStatements,
            withLocationReferences, cacheIndices);
        compiledDataContextStatements.Add(dataContextVariable);

        var expressionInvoke = new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeVariableReferenceExpression(dataContextVariableName), descriptor.GetMethodName));

        var returnStatement = new CodeMethodReturnStatement(expressionInvoke);

        compiledDataContextStatements.Add(returnStatement);

        var returnStatements = new CodeStatement[compiledDataContextStatements.Count];
        compiledDataContextStatements.CopyTo(returnStatements, 0);

        return returnStatements;
    }

    private CodeStatement[] GenerateStatementInvocation(CompiledExpressionDescriptor descriptor,
        bool withLocationReferences, IReadOnlyDictionary<string, int> cacheIndices)
    {
        var indexString = descriptor.Id.ToString(CultureInfo.InvariantCulture);
        var dataContextVariableName = "valDataContext" + indexString;

        var dataContextVariable = new CodeVariableDeclarationStatement(
            new CodeTypeReference(descriptor.TypeName), dataContextVariableName);

        var compiledDataContextStatements = new CodeStatementCollection();

        GenerateGetDataContextVariable(descriptor, dataContextVariable, compiledDataContextStatements,
            withLocationReferences, cacheIndices);
        compiledDataContextStatements.Add(dataContextVariable);

        var expressionInvoke = new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeVariableReferenceExpression(dataContextVariableName), descriptor.StatementMethodName));

        var returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(null));

        compiledDataContextStatements.Add(expressionInvoke);
        compiledDataContextStatements.Add(returnStatement);

        var returnStatements = new CodeStatement[compiledDataContextStatements.Count];
        compiledDataContextStatements.CopyTo(returnStatements, 0);

        return returnStatements;
    }

    private void GenerateCanExecuteMethod()
    {
        var canExecute = CanExecuteMethod();
        canExecute.Parameters.Insert(0,
            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Type)), "type"));
        //
        // if (((isReference == false)
        //              && ((expressionText == [expression text])
        //              && ([data context type name].Validate(locations, true) == true))))
        // {
        //     expressionId = [id for expression text and data context];
        //     return true;
        // }
        // 
        foreach (var descriptor in _expressionDescriptors)
        {
            var checkIsReferenceExpression = new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("isReference"),
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(descriptor.IsReference));

            var checkTypeExpression = new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("type"),
                CodeBinaryOperatorType.ValueEquality,
                new CodeTypeOfExpression(descriptor.ResultType));

            var checkTextExpression = new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("expressionText"),
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(descriptor.ExpressionText));

            var invokeValidateExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeTypeReferenceExpression(descriptor.TypeName),
                    "Validate"),
                new CodeVariableReferenceExpression("locations"),
                new CodePrimitiveExpression(true),
                new CodePrimitiveExpression(0));

            var checkValidateExpression = new CodeBinaryOperatorExpression(
                invokeValidateExpression,
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(true));

            var checkTextAndValidateExpression = new CodeBinaryOperatorExpression(
                checkTextExpression,
                CodeBinaryOperatorType.BooleanAnd,
                checkValidateExpression);

            CodeBinaryOperatorExpression checkIsReferenceAndTextAndValidateExpression = new(
                new CodeBinaryOperatorExpression(checkIsReferenceExpression, CodeBinaryOperatorType.BooleanAnd,
                    checkTypeExpression),
                CodeBinaryOperatorType.BooleanAnd,
                checkTextAndValidateExpression);

            var assignId = new CodeAssignStatement(
                new CodeVariableReferenceExpression("expressionId"),
                new CodePrimitiveExpression(descriptor.Id));

            var matchCondition = new CodeConditionStatement(
                checkIsReferenceAndTextAndValidateExpression);

            matchCondition.TrueStatements.Add(assignId);
            matchCondition.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));

            canExecute.Statements.Add(matchCondition);
        }

        canExecute.Statements.Add(
            new CodeAssignStatement(
                new CodeVariableReferenceExpression("expressionId"),
                new CodePrimitiveExpression(-1)));

        canExecute.Statements.Add(
            new CodeMethodReturnStatement(
                new CodePrimitiveExpression(false)));

        _classDeclaration.Members.Add(canExecute);

        var oldCanExecute = CanExecuteMethod();
        oldCanExecute.Statements.Add(
            new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(NotImplementedException))));
        _classDeclaration.Members.Add(oldCanExecute);
        return;

        static CodeMemberMethod CanExecuteMethod()
        {
            var canExecute = new CodeMemberMethod
            {
                Name = "CanExecuteExpression",
                ReturnType = new CodeTypeReference(typeof(bool)),
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            canExecute.CustomAttributes.Add(GeneratedCodeAttribute);
            canExecute.CustomAttributes.Add(BrowsableCodeAttribute);
            canExecute.CustomAttributes.Add(EditorBrowsableCodeAttribute);
            canExecute.ImplementationTypes.Add(new CodeTypeReference(typeof(ICompiledExpressionRoot)));

            canExecute.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "expressionText"));
            canExecute.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "isReference"));
            canExecute.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IList<LocationReference>)),
                    "locations"));

            var expressionIdParam =
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "expressionId");
            expressionIdParam.Direction = FieldDirection.Out;
            canExecute.Parameters.Add(expressionIdParam);
            return canExecute;
        }
    }

    private CodeObjectCreateExpression GenerateDataContextCreateExpression(string typeName, bool withLocationReferences)
    {
        if (withLocationReferences)
        {
            return new CodeObjectCreateExpression(
                new CodeTypeReference(typeName),
                new CodeVariableReferenceExpression("locations"),
                new CodeVariableReferenceExpression("activityContext"),
                new CodePrimitiveExpression(true));
        }

        return new CodeObjectCreateExpression(
            new CodeTypeReference(typeName), new CodeVariableReferenceExpression("locations"),
            new CodePrimitiveExpression(true));
    }

    private bool TryGenerateExpressionCode(Activity activity, CompiledDataContextDescriptor dataContextDescriptor,
        int nextExpressionId, string language)
    {
        var textExpression = (ITextExpression) activity;
        if (!TextExpression.LanguagesAreEqual(textExpression.Language, language)
            || string.IsNullOrWhiteSpace(textExpression.ExpressionText))
            //
            // We can only compile expressions that match the project's flavor
            // and expression activities with no expressions don't need anything generated.
        {
            return false;
        }

        var resultType = activity is ActivityWithResult result ? result.ResultType : null;

        var expressionText = textExpression.ExpressionText;

        var isReference = false;
        var isValue = false;
        var isStatement = false;

        if (resultType == null)
        {
            isStatement = true;
        }
        else
        {
            isReference = TypeHelper.AreTypesCompatible(resultType, typeof(Location));
            isValue = !isReference;
        }

        CodeTypeDeclaration typeDeclaration;
        if (isValue)
        {
            typeDeclaration = dataContextDescriptor.CodeTypeDeclarationForReadOnly;
        }
        else
            //
            // Statement and reference get read/write context
        {
            typeDeclaration = dataContextDescriptor.CodeTypeDeclaration;
        }

        var descriptor = new CompiledExpressionDescriptor
        {
            TypeName = typeDeclaration.Name,
            Id = nextExpressionId,
            ExpressionText = textExpression.ExpressionText
        };

        if (isReference)
        {
            if (resultType.IsGenericType)
            {
                resultType = resultType.GetGenericArguments()[0];
            }
            else
            {
                resultType = typeof(object);
            }
        }

        descriptor.ResultType = resultType;

        GenerateExpressionGetTreeMethod(activity, descriptor, dataContextDescriptor, isValue, isStatement,
            nextExpressionId);

        if (isValue || isReference)
        {
            var expressionGetMethod = GenerateGetMethod(activity, resultType, expressionText, nextExpressionId);
            typeDeclaration.Members.Add(expressionGetMethod);

            var expressionGetValueTypeAccessorMethod = GenerateGetMethodWrapper(expressionGetMethod);
            typeDeclaration.Members.Add(expressionGetValueTypeAccessorMethod);

            descriptor.GetMethodName = expressionGetValueTypeAccessorMethod.Name;
        }

        if (isReference)
        {
            var expressionSetMethod = GenerateSetMethod(activity, resultType, expressionText, nextExpressionId);
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(expressionSetMethod);

            var expressionSetValueTypeAccessorMethod = GenerateSetMethodWrapper(expressionSetMethod);
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(expressionSetValueTypeAccessorMethod);

            descriptor.SetMethodName = expressionSetValueTypeAccessorMethod.Name;
        }

        if (isStatement)
        {
            var statementMethod = GenerateStatementMethod(activity, expressionText, nextExpressionId);
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(statementMethod);

            var expressionSetValueTypeAccessorMethod = GenerateStatementMethodWrapper(statementMethod);
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(expressionSetValueTypeAccessorMethod);

            descriptor.StatementMethodName = expressionSetValueTypeAccessorMethod.Name;
        }

        _expressionDescriptors.Add(descriptor);

        return true;
    }

    private void GenerateExpressionGetTreeMethod(Activity activity, CompiledExpressionDescriptor expressionDescriptor,
        CompiledDataContextDescriptor dataContextDescriptor, bool isValue, bool isStatement, int nextExpressionId)
    {
        var expressionMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Assembly | MemberAttributes.Final,
            Name = string.Format(CultureInfo.InvariantCulture, ExpressionGetTreeString, nextExpressionId),
            ReturnType = new CodeTypeReference(typeof(Expression))
        };
        expressionDescriptor.GetExpressionTreeMethodName = expressionMethod.Name;

        if (isStatement)
        {
            // Can't generate expression tree for a statement
            expressionMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(expressionMethod);
            return;
        }

        var coreExpressionText = expressionDescriptor.ExpressionText;
        AlignText(activity, ref coreExpressionText, out var pragma);

        var returnType =
            typeof(Expression<>).MakeGenericType(typeof(Func<>).MakeGenericType(expressionDescriptor.ResultType));
        string expressionText = null;
        if (IsVb)
        {
            expressionText = string.Concat(VbLambdaString, coreExpressionText);
        }
        else if (IsCs)
        {
            expressionText = string.Concat(CSharpLambdaString, coreExpressionText);
        }

        if (expressionText != null)
        {
            var statement =
                new CodeVariableDeclarationStatement(returnType, "expression",
                    new CodeSnippetExpression(expressionText));
            statement.LinePragma = pragma;
            expressionMethod.Statements.Add(statement);

            var invokeExpression = new CodeMethodInvokeExpression(
                new CodeBaseReferenceExpression(),
                "RewriteExpressionTree", new CodeVariableReferenceExpression("expression"));

            expressionMethod.Statements.Add(new CodeMethodReturnStatement(invokeExpression));
        }
        else
        {
            expressionMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
        }

        if (isValue)
        {
            dataContextDescriptor.CodeTypeDeclarationForReadOnly.Members.Add(expressionMethod);
        }
        else
        {
            dataContextDescriptor.CodeTypeDeclaration.Members.Add(expressionMethod);
        }
    }

    private CodeMemberMethod GenerateGetMethod(Activity activity, Type resultType, string expressionText,
        int nextExpressionId)
    {
        var expressionMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = string.Format(CultureInfo.InvariantCulture, ExpressionGetString, nextExpressionId),
            ReturnType = new CodeTypeReference(resultType)
        };
        expressionMethod.CustomAttributes.Add(
            new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerHiddenAttribute))));

        AlignText(activity, ref expressionText, out var pragma);
        CodeStatement statement = new CodeMethodReturnStatement(new CodeSnippetExpression(expressionText));
        statement.LinePragma = pragma;
        expressionMethod.Statements.Add(statement);

        return expressionMethod;
    }

    private static CodeMemberMethod GenerateGetMethodWrapper(CodeMemberMethod expressionMethod)
    {
        var wrapperMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = ValueTypeAccessorString + expressionMethod.Name,
            ReturnType = expressionMethod.ReturnType
        };

        wrapperMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                GetValueTypeValuesString)));

        wrapperMethod.Statements.Add(new CodeMethodReturnStatement(
            new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeThisReferenceExpression(),
                    expressionMethod.Name))));

        return wrapperMethod;
    }

    private CodeMemberMethod GenerateSetMethod(Activity activity, Type resultType, string expressionText,
        int nextExpressionId)
    {
        var paramName = "value";

        if (string.Compare(expressionText, paramName, true, CultureInfo.CurrentCulture) == 0)
        {
            paramName += "1";
        }

        if (_settings.EnableFunctionParameterRename)
        {
            // Use a prefix and GUID to avoid conflicts with variables in the expression
            paramName = "param_" + Guid.NewGuid().ToString("N"); 
        }

        var expressionMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = string.Format(CultureInfo.InvariantCulture, ExpressionSetString, nextExpressionId)
        };
        expressionMethod.CustomAttributes.Add(
            new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerHiddenAttribute))));

        var exprValueParam = new CodeParameterDeclarationExpression(resultType, paramName);
        expressionMethod.Parameters.Add(exprValueParam);

        AlignText(activity, ref expressionText, out var pragma);
        var statement = new CodeAssignStatement(new CodeSnippetExpression(expressionText),
            new CodeArgumentReferenceExpression(paramName));
        statement.LinePragma = pragma;
        expressionMethod.Statements.Add(statement);

        return expressionMethod;
    }

    private static CodeMemberMethod GenerateSetMethodWrapper(CodeMemberMethod expressionMethod)
    {
        var wrapperMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = ValueTypeAccessorString + expressionMethod.Name
        };

        var exprValueParam = new CodeParameterDeclarationExpression(expressionMethod.Parameters[0].Type,
            expressionMethod.Parameters[0].Name);
        wrapperMethod.Parameters.Add(exprValueParam);

        wrapperMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                GetValueTypeValuesString)));

        var setExpression = new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                expressionMethod.Name));

        setExpression.Parameters.Add(new CodeVariableReferenceExpression(expressionMethod.Parameters[0].Name));

        wrapperMethod.Statements.Add(setExpression);

        wrapperMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                SetValueTypeValuesString)));

        return wrapperMethod;
    }

    private CodeMemberMethod GenerateStatementMethod(Activity activity, string expressionText, int nextExpressionId)
    {
        var expressionMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = string.Format(CultureInfo.InvariantCulture, ExpressionStatementString, nextExpressionId)
        };
        expressionMethod.CustomAttributes.Add(
            new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerHiddenAttribute))));

        AlignText(activity, ref expressionText, out var pragma);
        CodeStatement statement = new CodeSnippetStatement(expressionText);
        statement.LinePragma = pragma;
        expressionMethod.Statements.Add(statement);

        return expressionMethod;
    }

    private static CodeMemberMethod GenerateStatementMethodWrapper(CodeMemberMethod expressionMethod)
    {
        var wrapperMethod = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = ValueTypeAccessorString + expressionMethod.Name
        };

        wrapperMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                GetValueTypeValuesString)));

        var setExpression = new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                expressionMethod.Name));

        wrapperMethod.Statements.Add(setExpression);

        wrapperMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                SetValueTypeValuesString)));

        return wrapperMethod;
    }

    private static CodeMemberMethod GenerateGetValueTypeValues(CompiledDataContextDescriptor descriptor)
    {
        var fetchMethod = new CodeMemberMethod
        {
            Name = GetValueTypeValuesString,
            Attributes = MemberAttributes.Override | MemberAttributes.Family,
        };

        foreach (var (key, value) in descriptor.Fields)
        {
            if (descriptor.Duplicates.Contains(key))
            {
                continue;
            }

            CodeExpression getValue = new CodeCastExpression(
                value.Type,
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "GetVariableValue"),
                    new CodeBinaryOperatorExpression(
                        new CodePrimitiveExpression(value.Index),
                        CodeBinaryOperatorType.Add,
                        new CodeVariableReferenceExpression("locationsOffset"))));

            var fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), key);

            fetchMethod.Statements.Add(
                new CodeAssignStatement(fieldReference, getValue));
        }

        fetchMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeBaseReferenceExpression(),
                fetchMethod.Name)));

        return fetchMethod;
    }

    private static CodeMemberMethod GenerateSetValueTypeValues(CompiledDataContextDescriptor descriptor)
    {
        var pushMethod = new CodeMemberMethod
        {
            Name = SetValueTypeValuesString,
            Attributes = MemberAttributes.Override | MemberAttributes.Family
        };

        foreach (var (key, value) in descriptor.Fields)
        {
            if (descriptor.Duplicates.Contains(key) || value.IsReadonly)
            {
                continue;
            }

            var setValue = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeThisReferenceExpression(),
                    "SetVariableValue"),
                new CodeBinaryOperatorExpression(
                    new CodePrimitiveExpression(value.Index),
                    CodeBinaryOperatorType.Add,
                    new CodeVariableReferenceExpression("locationsOffset")),
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(), key));

            pushMethod.Statements.Add(setValue);
        }

        pushMethod.Statements.Add(new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
                new CodeBaseReferenceExpression(),
                pushMethod.Name)));

        return pushMethod;
    }

    private CodeTypeDeclaration GenerateCompiledDataContext(bool forReadOnly)
    {
        var forReadOnlyString = forReadOnly ? ForReadOnly : string.Empty;
        var contextName = string.Concat(_settings.ActivityName, TypedDataContextName, _nextContextId, forReadOnlyString);

        var typedDataContext = new CodeTypeDeclaration(contextName);
        typedDataContext.TypeAttributes = TypeAttributes.NestedPrivate;
        //
        // data context classes are declared inside of the main class via the partial class to reduce visibility/surface area.
        _classDeclaration.Members.Add(typedDataContext);

        if (_compiledDataContexts is {Count: > 0})
        {
            string baseTypeName = null;
            baseTypeName = forReadOnly
                ? _compiledDataContexts.Peek().CodeTypeDeclarationForReadOnly.Name
                : _compiledDataContexts.Peek().CodeTypeDeclaration.Name;

            typedDataContext.BaseTypes.Add(baseTypeName);
        }
        else
        {
            typedDataContext.BaseTypes.Add(typeof(CompiledDataContext));
            //
            // We only generate the helper method on the root data context/context 0
            // No need to have it on all contexts.  This is just a slight of hand
            // so that we don't need to make GetDataContextActivities public on CompiledDataContext.
            typedDataContext.Members.Add(GenerateDataContextActivitiesHelper());
        }

        var offsetField = new CodeMemberField
        {
            Attributes = MemberAttributes.Private,
            Name = LocationsOffsetFieldName,
            Type = new CodeTypeReference(typeof(int))
        };

        typedDataContext.Members.Add(offsetField);

        var expectedLocationsCountField = new CodeMemberField
        {
            Attributes = MemberAttributes.Private | MemberAttributes.Static,
            Name = ExpectedLocationsCountFieldName,
            Type = new CodeTypeReference(typeof(int))
        };

        typedDataContext.Members.Add(expectedLocationsCountField);

        typedDataContext.Members.Add(GenerateLocationReferenceActivityContextConstructor());
        typedDataContext.Members.Add(GenerateLocationConstructor());
        typedDataContext.Members.Add(GenerateLocationReferenceConstructor());
        typedDataContext.Members.Add(GenerateCacheHelper());
        typedDataContext.Members.Add(GenerateSetLocationsOffsetMethod());

        //
        // Mark this type as tool generated code
        typedDataContext.CustomAttributes.Add(GeneratedCodeAttribute);
        //
        // Mark it as Browsable(false) 
        // Note that this does not prevent intellisense within a single project, just at the metadata level            
        typedDataContext.CustomAttributes.Add(BrowsableCodeAttribute);
        //
        // Mark it as EditorBrowsable(EditorBrowsableState.Never)
        // Note that this does not prevent intellisense within a single project, just at the metadata level
        typedDataContext.CustomAttributes.Add(EditorBrowsableCodeAttribute);

        return typedDataContext;
    }

    private CodeMemberMethod GenerateDataContextActivitiesHelper()
    {
        var dataContextActivitiesHelper = new CodeMemberMethod
        {
            Name = "GetDataContextActivitiesHelper",
            Attributes = MemberAttributes.Assembly | MemberAttributes.Final | MemberAttributes.Static
        };

        if (_compiledDataContexts is {Count: > 0})
        {
            dataContextActivitiesHelper.Attributes |= MemberAttributes.New;
        }

        dataContextActivitiesHelper.ReturnType = new CodeTypeReference(typeof(object));

        dataContextActivitiesHelper.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(Activity)),
                "compiledRoot"));

        dataContextActivitiesHelper.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(bool)),
                ForImplementationName));

        dataContextActivitiesHelper.Statements.Add(
            new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(CompiledDataContext)),
                        "GetDataContextActivities"),
                    new CodeVariableReferenceExpression("compiledRoot"),
                    new CodeVariableReferenceExpression(ForImplementationName))));

        return dataContextActivitiesHelper;
    }

    private CodeMemberMethod GenerateSetLocationsOffsetMethod()
    {
        var setLocationsOffsetMethod = new CodeMemberMethod
        {
            Name = "SetLocationsOffset",
            Attributes = MemberAttributes.Public
        };
        setLocationsOffsetMethod.Parameters.Add(new CodeParameterDeclarationExpression(
            new CodeTypeReference(typeof(int)),
            "locationsOffsetValue"));
        if (_compiledDataContexts.Count > 0)
        {
            setLocationsOffsetMethod.Attributes |= MemberAttributes.New;
        }

        var assignLocationsOffsetStatement = new CodeAssignStatement(
            new CodeVariableReferenceExpression("locationsOffset"),
            new CodeVariableReferenceExpression("locationsOffsetValue"));
        setLocationsOffsetMethod.Statements.Add(assignLocationsOffsetStatement);

        if (_nextContextId > 0)
        {
            var baseSetLocationsOffsetMethod = new CodeMethodInvokeExpression(
                new CodeBaseReferenceExpression(), "SetLocationsOffset",
                new CodeVariableReferenceExpression("locationsOffset"));
            setLocationsOffsetMethod.Statements.Add(baseSetLocationsOffsetMethod);
        }

        return setLocationsOffsetMethod;
    }

    private CodeMemberMethod GenerateCacheHelper()
    {
        var cacheHelper = new CodeMemberMethod
        {
            Name = "GetCompiledDataContextCacheHelper",
            Attributes = MemberAttributes.Assembly | MemberAttributes.Final | MemberAttributes.Static
        };

        if (_compiledDataContexts is {Count: > 0})
        {
            cacheHelper.Attributes |= MemberAttributes.New;
        }

        cacheHelper.Parameters.Add(
            new CodeParameterDeclarationExpression(typeof(object), DataContextActivitiesFieldName));
        cacheHelper.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ActivityContext), "activityContext"));
        cacheHelper.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Activity), "compiledRoot"));
        cacheHelper.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), ForImplementationName));
        cacheHelper.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "compiledDataContextCount"));

        cacheHelper.ReturnType = new CodeTypeReference(typeof(CompiledDataContext[]));

        cacheHelper.Statements.Add(
            new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(CompiledDataContext)),
                        "GetCompiledDataContextCache"),
                    new CodeVariableReferenceExpression(DataContextActivitiesFieldName),
                    new CodeVariableReferenceExpression("activityContext"),
                    new CodeVariableReferenceExpression("compiledRoot"),
                    new CodeVariableReferenceExpression(ForImplementationName),
                    new CodeVariableReferenceExpression("compiledDataContextCount"))));

        return cacheHelper;
    }

    private CodeConstructor GenerateLocationReferenceActivityContextConstructor()
    {
        //
        // public [typename](IList<LocationReference> locations, ActivityContext activityContext)
        //   : base(locations, activityContext)
        //
        var constructor = new CodeConstructor();
        constructor.Attributes = MemberAttributes.Public;

        var constructorLocationsParam =
            new CodeParameterDeclarationExpression(typeof(IList<LocationReference>), "locations");
        constructor.Parameters.Add(constructorLocationsParam);

        constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("locations"));

        var constructorActivityContextParam =
            new CodeParameterDeclarationExpression(typeof(ActivityContext), "activityContext");
        constructor.Parameters.Add(constructorActivityContextParam);

        constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("activityContext"));

        var computelocationsOffsetParam =
            new CodeParameterDeclarationExpression(typeof(bool), "computelocationsOffset");
        constructor.Parameters.Add(computelocationsOffsetParam);

        if (_nextContextId > 0)
        {
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(false));
        }

        InvokeSetLocationsOffsetMethod(constructor);

        return constructor;
    }

    private CodeConstructor GenerateLocationConstructor()
    {
        //
        // public [typename](IList<Location> locations, ActivityContext activityContext)
        //   : base(locations)
        //
        var constructor = new CodeConstructor();
        constructor.Attributes = MemberAttributes.Public;

        var constructorLocationsParam =
            new CodeParameterDeclarationExpression(typeof(IList<Location>), "locations");
        constructor.Parameters.Add(constructorLocationsParam);

        constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("locations"));

        var computelocationsOffsetParam =
            new CodeParameterDeclarationExpression(typeof(bool), "computelocationsOffset");
        constructor.Parameters.Add(computelocationsOffsetParam);

        if (_nextContextId > 0)
        {
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(false));
        }

        InvokeSetLocationsOffsetMethod(constructor);

        return constructor;
    }

    private static CodeConstructor GenerateLocationReferenceConstructor()
    {
        //
        // public [typename](IList<LocationReference> locationReferences)
        //   : base(locationReferences)
        //
        var constructor = new CodeConstructor();
        constructor.Attributes = MemberAttributes.Public;

        var constructorLocationsParam =
            new CodeParameterDeclarationExpression(typeof(IList<LocationReference>), "locationReferences");
        constructor.Parameters.Add(constructorLocationsParam);

        constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("locationReferences"));

        return constructor;
    }

    private static void InvokeSetLocationsOffsetMethod(CodeConstructor constructor)
    {
        var setLocationsOffsetMethod = new CodeExpressionStatement(
            new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(),
                "SetLocationsOffset",
                new CodeBinaryOperatorExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("locations"), "Count"),
                    CodeBinaryOperatorType.Subtract,
                    new CodeVariableReferenceExpression("expectedLocationsCount"))));

        var offsetCheckStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(
            new CodeVariableReferenceExpression("computelocationsOffset"),
            CodeBinaryOperatorType.ValueEquality,
            new CodePrimitiveExpression(true)), setLocationsOffsetMethod);

        constructor.Statements.Add(offsetCheckStatement);
    }

    private CodeNamespace GenerateCodeNamespace()
    {
        var codeNamespace = new CodeNamespace(_settings.ActivityNamespace);

        var seenXamlIntegration = false;
        foreach (var nsReference in GetNamespaceReferences())
        {
            if (!seenXamlIntegration && nsReference == XamlIntegrationNamespace)
            {
                seenXamlIntegration = true;
            }

            codeNamespace.Imports.Add(new CodeNamespaceImport(nsReference)
            {
                LinePragma = GenerateLinePragmaForNamespace(nsReference)
            });
        }

        if (!seenXamlIntegration)
        {
            codeNamespace.Imports.Add(new CodeNamespaceImport(XamlIntegrationNamespace)
            {
                LinePragma = GenerateLinePragmaForNamespace(XamlIntegrationNamespace)
            });
        }

        return codeNamespace;
    }

    private bool AssemblyContainsTypeWithActivityNamespace()
    {
        // We need to include the ActivityNamespace in the imports if there are any types in
        // the Activity's assembly that are contained in that namespace.
        Type[] types;
        try
        {
            types = _settings.Activity.GetType().Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            // We had a problem loading all the types. Take the safe route and assume we need to include the ActivityNamespace.
            return true;
        }

        return types.Any(type => type.Namespace == _settings.ActivityNamespace);
    }

    private IEnumerable<string> GetNamespaceReferences()
    {
        var nsReferences = new HashSet<string>();
        // Add some namespace imports, use the same base set for C# as for VB, they aren't lang specific
        foreach (var nsReference in TextExpression.DefaultNamespaces)
        {
            nsReferences.Add(nsReference);
        }


        VisualBasicSettings vbSettings = null;
        if (IsVb)
        {
            vbSettings = VisualBasic.GetSettings(_settings.Activity);
        }

        if (vbSettings != null)
        {
            foreach (var nsReference in vbSettings.ImportReferences)
            {
                if (!string.IsNullOrWhiteSpace(nsReference.Import))
                {
                    // For VB, the ActivityNamespace has the RootNamespace stripped off. We don't need an Imports reference
                    // to ActivityNamespace, if this reference is in the same assembly and there is a RootNamespace specified.
                    // We check both Assembly.FullName and
                    // Assembly.GetName().Name because testing has shown that nsReference.Assembly sometimes gives fully qualified
                    // names and sometimes not.
                    if (
                        nsReference.Import == _settings.ActivityNamespace
                        &&
                        (nsReference.Assembly == _settings.Activity.GetType().Assembly.FullName ||
                        nsReference.Assembly == _settings.Activity.GetType().Assembly.GetName().Name)
                        &&
                        !string.IsNullOrWhiteSpace(_settings.RootNamespace)
                        &&
                        !AssemblyContainsTypeWithActivityNamespace()
                        )
                    {
                        continue;
                    }

                    nsReferences.Add(nsReference.Import);
                }
            }
        }
        else
        {
            var references = _settings.ForImplementation
                ? TextExpression.GetNamespacesForImplementation(_settings.Activity)
                : TextExpression.GetNamespaces(_settings.Activity);

            foreach (var nsReference in references)
            {
                if (!string.IsNullOrWhiteSpace(nsReference))
                {
                    nsReferences.Add(nsReference);
                }
            }
        }

        return nsReferences;
    }

    private CompiledDataContextDescriptor PushDataContextDescriptor()
    {
        var contextDescriptor = new CompiledDataContextDescriptor(() => IsVb)
        {
            CodeTypeDeclaration = GenerateCompiledDataContext(false),
            CodeTypeDeclarationForReadOnly = GenerateCompiledDataContext(true),
            NextMemberIndex = GetStartMemberIndex()
        };
        _compiledDataContexts.Push(contextDescriptor);
        _nextContextId++;

        return contextDescriptor;
    }

    private void PopDataContextDescriptor()
    {
        var descriptor = _compiledDataContexts.Pop();
        if (descriptor != null)
        {
            GenerateMembers(descriptor);
            GenerateValidate(descriptor, true);
            GenerateValidate(descriptor, false);
        }
    }

    private int GetStartMemberIndex()
    {
        if (_compiledDataContexts == null || _compiledDataContexts.Count == 0)
        {
            return 0;
        }

        return _compiledDataContexts.Peek().NextMemberIndex;
    }

    private void GenerateValidate(CompiledDataContextDescriptor descriptor, bool forReadOnly)
    {
        //
        //
        // Validate the locations at runtime match the set at compile time
        //
        // protected override bool Validate(IList<LocationReference> locationReferences)
        // {
        //   if (validateLocationCount && locationReferences.Count != [generated count of location references])
        //   {
        //     return false;
        //   }
        //   if (locationReferences[0].Name != [generated name for index] ||
        //       locationReferences[0].Type != typeof([generated type for index]))
        //   {
        //     return false;
        //   }
        //
        //   ...
        //
        // }
        var validateMethod = new CodeMemberMethod
        {
            Name = "Validate",
            Attributes = MemberAttributes.Public | MemberAttributes.Static
        };

        if (_compiledDataContexts.Count > 0)
        {
            validateMethod.Attributes |= MemberAttributes.New;
        }

        validateMethod.ReturnType = new CodeTypeReference(typeof(bool));

        validateMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(IList<LocationReference>)),
                "locationReferences"));

        validateMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(bool)),
                "validateLocationCount"));

        validateMethod.Parameters.Add(
            new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(int)),
                "offset"));

        var shouldCheckLocationCountExpression = new CodeBinaryOperatorExpression(
            new CodeVariableReferenceExpression("validateLocationCount"),
            CodeBinaryOperatorType.ValueEquality,
            new CodePrimitiveExpression(true));

        var compareLocationCountExpression = new CodeBinaryOperatorExpression(
            new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression("locationReferences"),
                "Count"),
            CodeBinaryOperatorType.LessThan,
            new CodePrimitiveExpression(descriptor.NextMemberIndex)
            );

        var checkLocationCountExpression = new CodeBinaryOperatorExpression(
            shouldCheckLocationCountExpression,
            CodeBinaryOperatorType.BooleanAnd,
            compareLocationCountExpression);

        var checkLocationCountStatement = new CodeConditionStatement(
            checkLocationCountExpression,
            new CodeMethodReturnStatement(
                new CodePrimitiveExpression(false)));

        validateMethod.Statements.Add(checkLocationCountStatement);

        if (descriptor.NextMemberIndex > 0)
        {
            var generateNewOffset = new CodeConditionStatement(shouldCheckLocationCountExpression,
                new CodeAssignStatement(new CodeVariableReferenceExpression("offset"),
                    new CodeBinaryOperatorExpression(
                        new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("locationReferences"),
                            "Count"),
                        CodeBinaryOperatorType.Subtract,
                        new CodePrimitiveExpression(descriptor.NextMemberIndex))));
            validateMethod.Statements.Add(generateNewOffset);
        }

        var setexpectedLocationsCountStatement = new CodeAssignStatement(
            new CodeVariableReferenceExpression("expectedLocationsCount"),
            new CodePrimitiveExpression(descriptor.NextMemberIndex));

        validateMethod.Statements.Add(setexpectedLocationsCountStatement);

        foreach (var kvp in descriptor.Properties)
        {
            validateMethod.Statements.Add(GenerateLocationReferenceCheck(kvp.Value));
        }

        foreach (var kvp in descriptor.Fields)
        {
            validateMethod.Statements.Add(GenerateLocationReferenceCheck(kvp.Value));
        }

        if (_compiledDataContexts.Count >= 1)
        {
            var baseDescriptor = _compiledDataContexts.Peek();
            var baseType = forReadOnly
                ? baseDescriptor.CodeTypeDeclarationForReadOnly
                : baseDescriptor.CodeTypeDeclaration;

            var invokeBase = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeTypeReferenceExpression(baseType.Name),
                    "Validate"),
                new CodeVariableReferenceExpression("locationReferences"),
                new CodePrimitiveExpression(false),
                new CodeVariableReferenceExpression("offset"));

            validateMethod.Statements.Add(
                new CodeMethodReturnStatement(invokeBase));
        }
        else
        {
            validateMethod.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodePrimitiveExpression(true)));
        }

        if (forReadOnly)
        {
            descriptor.CodeTypeDeclarationForReadOnly.Members.Add(validateMethod);
        }
        else
        {
            descriptor.CodeTypeDeclaration.Members.Add(validateMethod);
        }
    }

    private CodeConditionStatement GenerateLocationReferenceCheck(MemberData memberData)
    {
        var indexer = new CodeIndexerExpression(
            new CodeVariableReferenceExpression("locationReferences"),
            new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("offset"),
                CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(memberData.Index)));

        var locationNameExpression = new CodeBinaryOperatorExpression(
            new CodePropertyReferenceExpression(indexer, "Name"),
            CodeBinaryOperatorType.IdentityInequality,
            new CodePrimitiveExpression(memberData.Name));

        var locationTypeExpression = new CodeBinaryOperatorExpression(
            new CodePropertyReferenceExpression(indexer, "Type"),
            CodeBinaryOperatorType.IdentityInequality,
            new CodeTypeOfExpression(memberData.Type));

        var locationExpression = new CodeBinaryOperatorExpression(
            locationNameExpression,
            CodeBinaryOperatorType.BooleanOr,
            locationTypeExpression);

        var returnStatement = new CodeMethodReturnStatement
        {
            Expression = new CodePrimitiveExpression(false)
        };

        var locationStatement = new CodeConditionStatement(
            locationExpression,
            returnStatement);

        return locationStatement;
    }

    private void WriteCode(TextWriter textWriter)
    {
        using var codeDomProvider = CodeDomProvider.CreateProvider(_settings.Language);
        using var indentedTextWriter = new IndentedTextWriter(textWriter);
        codeDomProvider.GenerateCodeFromNamespace(_codeNamespace, indentedTextWriter,
            new CodeGeneratorOptions());
    }

    private TextExpressionCompilerResults CompileInMemory()
    {
        var messages = new List<TextExpressionCompilerError>();
        var references = GetReferences(messages);
        var code = _compileUnit.GetCode(_settings.Language);
        var imports = _compileUnit.GetImports();
        var classToCompile = new ClassToCompile(_settings.ActivityName, code, references, imports);
        var result = _settings.Compiler.Compile(classToCompile);
        result.AddMessages(messages);
        return result;
    }

    private HashSet<Assembly> GetReferences(ICollection<TextExpressionCompilerError> messages)
    {
        List<AssemblyReference> assemblies;
        if (IsVb)
        {
            JitCompilerHelper.GetAllImportReferences(_settings.Activity, false, out _, out assemblies);
        }
        else
        {
            assemblies = _settings.ForImplementation
                ? new List<AssemblyReference>(TextExpression.GetReferencesForImplementation(_settings.Activity))
                : new List<AssemblyReference>(TextExpression.GetReferences(_settings.Activity));
            assemblies.AddRange(TextExpression.DefaultReferences);
        }

        var references = new HashSet<Assembly>();
        foreach (var assemblyReference in assemblies)
        {
            if (assemblyReference == null)
            {
                continue;
            }

            assemblyReference.LoadAssembly();
            if (assemblyReference.Assembly == null)
            {
                var warning = new TextExpressionCompilerError
                {
                    IsWarning = true,
                    Message = SR.TextExpressionCompilerUnableToLoadAssembly(assemblyReference.AssemblyName)
                };
                messages.Add(warning);
                continue;
            }

            references.Add(assemblyReference.Assembly);
        }

        return references;
    }

    private static void AlignText(Activity activity, ref string expressionText, out CodeLinePragma pragma)
    {
        pragma = null;
    }

    private CodeLinePragma GenerateLinePragmaForNamespace(string namespaceName)
    {
        if (_fileName == null)
        {
            return null;
        }

        // if source xaml file doesn't exist or it doesn't contain TextExpression
        // it defaults to line number 1
        var lineNumber = 1;
        var lineNumberDictionary = _settings.ForImplementation ? _lineNumbersForNSesForImpl : _lineNumbersForNSes;

        if (lineNumberDictionary.TryGetValue(namespaceName, out var lineNumReturned))
        {
            lineNumber = lineNumReturned;
        }

        return new CodeLinePragma(_fileName, lineNumber);
    }

    private string GetActivityFullName(TextExpressionCompilerSettings settings)
    {
        string rootNamespacePrefix = null;
        string namespacePrefix = null;
        var activityFullName = "";
        if (IsVb && !string.IsNullOrWhiteSpace(_settings.RootNamespace))
        {
            rootNamespacePrefix = _settings.RootNamespace + ".";
        }

        if (!string.IsNullOrWhiteSpace(_settings.ActivityNamespace))
        {
            namespacePrefix = _settings.ActivityNamespace + ".";
        }

        if (rootNamespacePrefix != null)
        {
            if (namespacePrefix != null)
            {
                activityFullName = rootNamespacePrefix + namespacePrefix + settings.ActivityName;
            }
            else
            {
                activityFullName = rootNamespacePrefix + settings.ActivityName;
            }
        }
        else
        {
            if (namespacePrefix != null)
            {
                activityFullName = namespacePrefix + settings.ActivityName;
            }
            else
            {
                activityFullName = settings.ActivityName;
            }
        }

        return activityFullName;
    }

    private class ExpressionCompilerActivityVisitor : CompiledExpressionActivityVisitor
    {
        private readonly TextExpressionCompiler _compiler;

        public ExpressionCompilerActivityVisitor(TextExpressionCompiler compiler)
        {
            _compiler = compiler;
        }

        public int NextExpressionId { get; set; }

        protected override void VisitRoot(Activity activity, out bool exit)
        {
            _compiler.OnRootActivity();

            base.VisitRoot(activity, out exit);

            _compiler.OnAfterRootActivity();
        }

        protected override void VisitRootImplementationArguments(Activity activity, out bool exit)
        {
            base.VisitRootImplementationArguments(activity, out exit);

            if (ForImplementation)
            {
                _compiler.OnAfterRootArguments(activity);
            }
        }

        protected override void VisitVariableScope(Activity activity, out bool exit)
        {
            _compiler.OnVariableScope(activity);

            base.VisitVariableScope(activity, out exit);
            _compiler.OnAfterVariableScope();
        }

        protected override void VisitRootImplementationScope(Activity activity, out bool exit)
        {
            _compiler.OnRootImplementationScope(activity, out var rootArgumentAccessorContext);

            base.VisitRootImplementationScope(activity, out exit);

            _compiler.OnAfterRootImplementationScope(activity, rootArgumentAccessorContext);
        }

        protected override void VisitVariableScopeArgument(RuntimeArgument runtimeArgument, out bool exit)
        {
            _compiler.InVariableScopeArgument = true;
            base.VisitVariableScopeArgument(runtimeArgument, out exit);
            _compiler.InVariableScopeArgument = false;
        }

        protected override void VisitITextExpression(Activity activity, out bool exit)
        {
            _compiler.OnITextExpressionFound(activity, this);
            exit = false;
        }

        protected override void VisitDelegate(ActivityDelegate activityDelegate, out bool exit)
        {
            _compiler.OnActivityDelegateScope();

            base.VisitDelegate(activityDelegate, out exit);

            _compiler.OnAfterActivityDelegateScope();

            exit = false;
        }

        protected override void VisitDelegateArgument(RuntimeDelegateArgument delegateArgument, out bool exit)
        {
            _compiler.OnDelegateArgument(delegateArgument);

            base.VisitDelegateArgument(delegateArgument, out exit);
        }
    }

    private class CompiledExpressionDescriptor
    {
        internal bool IsValue =>
            !string.IsNullOrWhiteSpace(GetMethodName) &&
            string.IsNullOrWhiteSpace(SetMethodName) &&
            string.IsNullOrWhiteSpace(StatementMethodName);

        internal bool IsReference => !string.IsNullOrWhiteSpace(SetMethodName);

        internal bool IsStatement => !string.IsNullOrWhiteSpace(StatementMethodName);

        internal string TypeName { get; set; }

        internal Type ResultType { get; set; }

        internal string GetMethodName { get; set; }

        internal string SetMethodName { get; set; }

        internal string StatementMethodName { get; set; }

        internal int Id { get; set; }

        internal string ExpressionText { get; set; }

        internal string GetExpressionTreeMethodName { get; set; }
    }

    private class CompiledDataContextDescriptor
    {
        private readonly Func<bool> _isVb;
        private ISet<string> _duplicates;
        private IDictionary<string, MemberData> _fields;
        private IDictionary<string, MemberData> _properties;

        public CompiledDataContextDescriptor(Func<bool> isVb)
        {
            _isVb = isVb;
        }

        public IDictionary<string, MemberData> Fields =>
            _fields ??= _isVb()
                ? new Dictionary<string, MemberData>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, MemberData>();

        public IDictionary<string, MemberData> Properties =>
            _properties ??= _isVb()
                ? new Dictionary<string, MemberData>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, MemberData>();

        public ISet<string> Duplicates =>
            _duplicates ??= _isVb() 
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                : new HashSet<string>();

        public CodeTypeDeclaration CodeTypeDeclaration { get; set; }

        public CodeTypeDeclaration CodeTypeDeclarationForReadOnly { get; set; }

        public int NextMemberIndex { get; set; }
    }

    private struct MemberData
    {
        public int Index;
        public string Name;
        public Type Type;
        public bool IsReadonly;
    }
}
