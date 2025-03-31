// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xaml;
using System.Xml;

namespace System.Activities.XamlIntegration;

public static class ActivityXamlServices
{
    private static readonly XamlSchemaContext s_dynamicActivityReaderSchemaContext =
        new DynamicActivityReaderSchemaContext();

    public static Activity Load(Stream stream)
    {
        if (stream == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(stream));
        }

        return Load(stream, new ActivityXamlServicesSettings());
    }

    public static Activity Load(Stream stream, ActivityXamlServicesSettings settings)
    {
        if (stream == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(stream));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        using var xmlReader = XmlReader.Create(stream);
        return Load(xmlReader, settings);
    }

    public static Activity Load(string fileName)
    {
        if (fileName == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(fileName));
        }

        return Load(fileName, new ActivityXamlServicesSettings());
    }

    public static Activity Load(string fileName, ActivityXamlServicesSettings settings)
    {
        if (fileName == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(fileName));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        using var xmlReader = XmlReader.Create(fileName);
        return Load(xmlReader, settings);
    }

    public static Activity Load(TextReader textReader)
    {
        if (textReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(textReader));
        }

        return Load(textReader, new ActivityXamlServicesSettings());
    }

    public static Activity Load(TextReader textReader, ActivityXamlServicesSettings settings)
    {
        if (textReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(textReader));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        using var xmlReader = XmlReader.Create(textReader);
        return Load(xmlReader, settings);
    }

    public static Activity Load(XmlReader xmlReader)
    {
        if (xmlReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(xmlReader));
        }

        return Load(xmlReader, new ActivityXamlServicesSettings());
    }

    public static Activity Load(XmlReader xmlReader, ActivityXamlServicesSettings settings)
    {
        if (xmlReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(xmlReader));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        using var xamlReader = new XamlXmlReader(xmlReader, s_dynamicActivityReaderSchemaContext);
        return Load(xamlReader, settings);
    }

    public static Activity Load(XamlReader xamlReader)
    {
        if (xamlReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(xamlReader));
        }

        return Load(xamlReader, new ActivityXamlServicesSettings());
    }

    public static Activity Load(XamlReader xamlReader, ActivityXamlServicesSettings settings)
    {
        if (xamlReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(xamlReader));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        var dynamicActivityReader = new DynamicActivityXamlReader(xamlReader);
        var xamlObject = XamlServices.Load(dynamicActivityReader);
        if (!(xamlObject is Activity result))
        {
            throw FxTrace.Exception.Argument("reader", SR.ActivityXamlServicesRequiresActivity(
                xamlObject != null ? xamlObject.GetType().FullName : string.Empty));
        }

        if (result is IDynamicActivity dynamicActivity && settings.CompileExpressions)
        {
            Compile(dynamicActivity, settings);
        }

        return result;
    }

    public static XamlReader CreateReader(Stream stream)
    {
        if (stream == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(stream));
        }

        return CreateReader(new XamlXmlReader(XmlReader.Create(stream), s_dynamicActivityReaderSchemaContext),
            s_dynamicActivityReaderSchemaContext);
    }

    public static XamlReader CreateReader(XamlReader innerReader)
    {
        if (innerReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
        }

        return new DynamicActivityXamlReader(innerReader);
    }

    public static XamlReader CreateReader(XamlReader innerReader, XamlSchemaContext schemaContext)
    {
        if (innerReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
        }

        if (schemaContext == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(schemaContext));
        }

        return new DynamicActivityXamlReader(innerReader, schemaContext);
    }

    public static XamlReader CreateBuilderReader(XamlReader innerReader)
    {
        if (innerReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
        }

        return new DynamicActivityXamlReader(true, innerReader, null);
    }

    public static XamlReader CreateBuilderReader(XamlReader innerReader, XamlSchemaContext schemaContext)
    {
        if (innerReader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
        }

        if (schemaContext == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(schemaContext));
        }

        return new DynamicActivityXamlReader(true, innerReader, schemaContext);
    }

    public static XamlWriter CreateBuilderWriter(XamlWriter innerWriter)
    {
        if (innerWriter == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(innerWriter));
        }

        return new ActivityBuilderXamlWriter(innerWriter);
    }

    public static Func<object> CreateFactory(XamlReader reader, Type resultType)
    {
        if (reader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(reader));
        }

        if (resultType == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(resultType));
        }

        return FuncFactory.CreateFunc(reader, resultType);
    }

    public static Func<T> CreateFactory<T>(XamlReader reader) where T : class
    {
        if (reader == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(reader));
        }

        return FuncFactory.CreateFunc<T>(reader);
    }

    public static void Compile(IDynamicActivity dynamicActivity, ActivityXamlServicesSettings settings)
    {
        if (!RequiresCompilation(dynamicActivity, settings.LocationReferenceEnvironment, out var language))
        {
            return;
        }

        var aotCompiler = settings.GetCompiler(language);
        var compilerSettings = GetCompilerSettings(dynamicActivity, language, aotCompiler);
        compilerSettings.EnableFunctionParameterRename = settings.EnableFunctionParameterRename;
        var compiler = new TextExpressionCompiler(compilerSettings);
        
        var results = compiler.Compile();

        if (results.HasErrors)
        {
            var messages = new StringBuilder();
            foreach (var message in results.CompilerMessages)
            {
                messages.Append("\r\n");
                messages.Append(string.Concat(" ", SR.ActivityXamlServiceLineString, " ", message.SourceLineNumber,
                    ": "));
                messages.Append(message.Message);
            }

            messages.Append("\r\n");

            var exception =
                new InvalidOperationException(SR.ActivityXamlServicesCompilationFailed(messages.ToString()));

            foreach (var message in results.CompilerMessages)
            {
                exception.Data.Add(message, message.Message);
            }

            throw FxTrace.Exception.AsError(exception);
        }

        var compiledExpressionRootType = results.ResultType;

        var compiledExpressionRoot =
            Activator.CreateInstance(compiledExpressionRootType, dynamicActivity) as ICompiledExpressionRoot;
        CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(dynamicActivity, compiledExpressionRoot);
    }

    private static bool RequiresCompilation(IDynamicActivity dynamicActivity, LocationReferenceEnvironment environment,
        out string language)
    {
        language = null;

        if (!((Activity) dynamicActivity).IsMetadataCached)
        {
            IList<ValidationError> validationErrors = null;
            environment ??= new ActivityLocationReferenceEnvironment {CompileExpressions = true};

            try
            {
                ActivityUtilities.CacheRootMetadata((Activity) dynamicActivity, environment,
                    ProcessActivityTreeOptions.FullCachingOptions, null, ref validationErrors);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                throw FxTrace.Exception.AsError(new InvalidOperationException(
                    SR.CompiledExpressionsCacheMetadataException(dynamicActivity.Name, e.ToString())));
            }
        }

        var visitor = new DynamicActivityVisitor();
        visitor.Visit((Activity) dynamicActivity, true);

        if (!visitor.RequiresCompilation)
        {
            return false;
        }

        if (visitor.HasLanguageConflict)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(
                SR.DynamicActivityMultipleExpressionLanguages(visitor.GetConflictingLanguages()
                                                                    .AsCommaSeparatedValues())));
        }

        language = visitor.Language;
        return true;
    }

    private static TextExpressionCompilerSettings GetCompilerSettings(IDynamicActivity dynamicActivity, string language,
        AheadOfTimeCompiler compiler)
    {
        var activity = (Activity) dynamicActivity;
        var name = dynamicActivity.Name ?? activity.DisplayName;
        var lastIndexOfDot = name.LastIndexOf('.');
        var lengthOfName = name.Length;

        var activityName = lastIndexOfDot > 0 ? name.Substring(lastIndexOfDot + 1) : name;
        activityName += "_CompiledExpressionRoot";
        var activityNamespace = lastIndexOfDot > 0 ? name.Substring(0, lastIndexOfDot) : null;

        return new TextExpressionCompilerSettings
        {
            Activity = activity,
            ActivityName = activityName,
            ActivityNamespace = activityNamespace,
            RootNamespace = null,
            GenerateAsPartialClass = false,
            AlwaysGenerateSource = true,
            Language = language,
            Compiler = compiler
        };
    }

    public static void InitializeComponent(
        Type componentType,
        object componentInstance
        )
    {
        if (componentType == null)
        {
            throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(componentType)));
        }

        if (componentInstance == null)
        {
            throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(componentInstance)));
        }

        var typesAssembly = componentType.Assembly;

        // Get the set of resources from the type's assembly.
        var typeName = componentType.Name;
        var typeNamespace = componentType.Namespace;
        var resources = typesAssembly.GetManifestResourceNames();

        // Look for the special resource that is generated by the BeforeInitializeComponentExtension.
        string beforeInitializeResourceName;
        if (string.IsNullOrWhiteSpace(typeNamespace))
        {
            beforeInitializeResourceName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.{2}", typeName,
                "BeforeInitializeComponentHelper", "txt");
        }
        else
        {
            beforeInitializeResourceName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}.{3}", typeNamespace,
                typeName, "BeforeInitializeComponentHelper", "txt");
        }

        var beforeInitializeResource = FindResource(resources, beforeInitializeResourceName);
        if (beforeInitializeResource == null)
        {
            throw FxTrace.Exception.AsError(
                new InvalidOperationException(SR.BeforeInitializeComponentXBTExtensionResourceNotFound));
        }

        GetContentsOfBeforeInitializeExtensionResource(typesAssembly, beforeInitializeResource,
            out var xamlResourceName, out var helperClassName);

        // Now look for the resource containing the XAML.
        var fullXamlResourceName = FindResource(resources, xamlResourceName);
        if (fullXamlResourceName == null)
        {
            throw FxTrace.Exception.AsError(
                new InvalidOperationException(SR.XamlBuildTaskResourceNotFound(xamlResourceName)));
        }

        // Get the schema context for the type.
        var typeSchemaContext = GetXamlSchemaContext(typesAssembly, helperClassName);

        InitializeComponentFromXamlResource(componentType, fullXamlResourceName, componentInstance, typeSchemaContext);
    }

    private static string FindResource(IReadOnlyList<string> resources, string partialResourceName)
    {
        var foundResourceString = false;
        int resourceIndex;
        for (resourceIndex = 0; resourceIndex < resources.Count; resourceIndex = resourceIndex + 1)
        {
            var resource = resources[resourceIndex];
            if (resource.Contains("." + partialResourceName) || resource.Equals(partialResourceName))
            {
                foundResourceString = true;
                break;
            }
        }

        return foundResourceString ? resources[resourceIndex] : null;
    }

    private static void GetContentsOfBeforeInitializeExtensionResource(Assembly assembly, string resource,
        out string xamlResourceName, out string helperClassName)
    {
        var beforeInitializeStream = assembly.GetManifestResourceStream(resource);
        Fx.Assert(beforeInitializeStream != null, nameof(beforeInitializeStream) + " != null");
        using var beforeInitializeReader = new StreamReader(beforeInitializeStream);
        xamlResourceName = beforeInitializeReader.ReadLine();
        helperClassName = beforeInitializeReader.ReadLine();
    }

    private static XamlSchemaContext GetXamlSchemaContext(Assembly assembly, string helperClassName)
    {
        XamlSchemaContext typeSchemaContext = null;
        var schemaContextType = assembly.GetType(helperClassName);
        if (schemaContextType == null)
        {
            throw FxTrace.Exception.AsError(
                new InvalidOperationException(
                    SR.SchemaContextFromBeforeInitializeComponentXBTExtensionNotFound(helperClassName)));
        }

        // The "official" BeforeInitializeComponent XBT Extension will not create a generic type for this helper class.
        // This check is here so that the assembly manifest can't lure us into creating a type with a generic argument from a different assembly.
        if (schemaContextType.IsGenericType || schemaContextType.IsGenericTypeDefinition)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(
                SR.SchemaContextFromBeforeInitializeComponentXBTExtensionCannotBeGeneric(helperClassName)));
        }

        var schemaContextPropertyInfo = schemaContextType.GetProperty("SchemaContext",
            BindingFlags.NonPublic | BindingFlags.Static);
        typeSchemaContext = (XamlSchemaContext) schemaContextPropertyInfo?.GetValue(null,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty, null, null, null);
        return typeSchemaContext;
    }

    private static void InitializeComponentFromXamlResource(Type componentType, string resource,
        object componentInstance, XamlSchemaContext schemaContext)
    {
        var initializeXaml = componentType.Assembly.GetManifestResourceStream(resource);
        Fx.Assert(initializeXaml != null, nameof(initializeXaml) + " != null");
        XmlReader xmlReader = null;
        XamlReader reader = null;
        XamlObjectWriter objectWriter = null;
        try
        {
            xmlReader = XmlReader.Create(initializeXaml);
            var readerSettings = new XamlXmlReaderSettings
            {
                LocalAssembly = componentType.Assembly,
                AllowProtectedMembersOnRoot = true
            };
            reader = new XamlXmlReader(xmlReader, schemaContext, readerSettings);
            var writerSettings = new XamlObjectWriterSettings
            {
                RootObjectInstance = componentInstance
            };
            //writerSettings.AccessLevel = XamlAccessLevel.PrivateAccessTo(componentType);
            objectWriter = new XamlObjectWriter(schemaContext, writerSettings);

            // We need the XamlLoadPermission for the assembly we are dealing with.
            //XamlLoadPermission perm = new XamlLoadPermission(XamlAccessLevel.PrivateAccessTo(componentType));
            //perm.Assert();
            XamlServices.Transform(reader, objectWriter);
        }
        finally
        {
            ((IDisposable) xmlReader)?.Dispose();

            ((IDisposable) reader)?.Dispose();

            ((IDisposable) objectWriter)?.Dispose();
        }
    }

    private class DynamicActivityReaderSchemaContext : XamlSchemaContext
    {
        private const string ServiceModelDll =
            "System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

        private const string ServiceModelActivitiesDll =
            "System.ServiceModel.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

        private const string ServiceModelNamespace = "http://schemas.microsoft.com/netfx/2009/xaml/servicemodel";
        private static bool s_serviceModelLoaded;

        // Eventually this will be unnecessary since XAML team has changed the default behavior
        public DynamicActivityReaderSchemaContext()
            : base(new XamlSchemaContextSettings()) { }

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            var xamlType = base.GetXamlType(xamlNamespace, name, typeArguments);

            if (xamlType == null && xamlNamespace == ServiceModelNamespace && !s_serviceModelLoaded)
            {
                Assembly.Load(ServiceModelDll);
                Assembly.Load(ServiceModelActivitiesDll);
                s_serviceModelLoaded = true;
                xamlType = base.GetXamlType(xamlNamespace, name, typeArguments);
            }

            return xamlType;
        }
    }

    private class DynamicActivityVisitor : CompiledExpressionActivityVisitor
    {
        private ISet<string> _languages;

        public string Language
        {
            get
            {
                if (_languages == null || _languages.Count is 0 or > 1)
                {
                    return null;
                }

                using var languagesEnumerator = _languages.GetEnumerator();

                return languagesEnumerator.MoveNext() ? languagesEnumerator.Current : null;
            }
        }

        public bool RequiresCompilation { get; private set; }

        public bool HasLanguageConflict => _languages is {Count: > 1};

        public IEnumerable<string> GetConflictingLanguages() => _languages.Count > 1 ? _languages : null;

        protected override void VisitITextExpression(Activity activity, out bool exit)
        {
            if (activity is ITextExpression textExpression)
            {
                RequiresCompilation = true;

                _languages ??= new HashSet<string>();

                if (!_languages.Contains(textExpression.Language))
                {
                    _languages.Add(textExpression.Language);
                }
            }

            base.VisitITextExpression(activity, out exit);
        }
    }
}
