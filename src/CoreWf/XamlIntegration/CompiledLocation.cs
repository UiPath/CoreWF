// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;

namespace System.Activities.XamlIntegration;

[DataContract(Name = XD.CompiledLocation.Name, Namespace = XD.Runtime.Namespace)]
internal class CompiledLocation<T> : Location<T>
{
    private Func<T> _getMethod;
    private Action<T> _setMethod;
    private int _expressionId;
    private IList<LocationReference> _locationReferences;
    private IList<Location> _locations;
    private ActivityInstance _rootInstance;
    private readonly Activity _compiledRootActivity;
    private byte[] _compiledRootActivityQualifiedId;
    private readonly Activity _expressionActivity;
    private byte[] _expressionActivityQualifiedId;
    private string _expressionText;
    private bool _forImplementation;

    public CompiledLocation(
        Func<T> getMethod,
        Action<T> setMethod,
        IList<LocationReference> locationReferences,
        IList<Location> locations,
        int expressionId,
        Activity compiledRootActivity,
        ActivityContext currentActivityContext)
    {
        _getMethod = getMethod;
        _setMethod = setMethod;

        _forImplementation = currentActivityContext.Activity.MemberOf != currentActivityContext.Activity.RootActivity.MemberOf;
        _locationReferences = locationReferences;
        _locations = locations;
        _expressionId = expressionId;

        _compiledRootActivity = compiledRootActivity;
        _expressionActivity = currentActivityContext.Activity;
        //
        // Save the root activity instance to get the root activity post persistence
        // The root will always be alive as long as the location is valid, which is not
        // true for the activity instance of the expression that is executing
        _rootInstance = currentActivityContext.CurrentInstance;
        while (_rootInstance.Parent != null)
        {
            _rootInstance = _rootInstance.Parent;
        }
        //
        // Save the text of the expression for exception message
        if (currentActivityContext.Activity is ITextExpression textExpression)
        {
            _expressionText = textExpression.ExpressionText;
        }
    }

    public CompiledLocation(Func<T> getMethod, Action<T> setMethod)
    {
        //
        // This is the constructor that is used to refresh the get/set methods during rehydration
        // An instance of this class created with the constructor cannot be invoked.
        _getMethod = getMethod;
        _setMethod = setMethod;
    }

    public override T Value
    {
        get
        {
            if (_getMethod == null)
            {
                RefreshAccessors();
            }
            return _getMethod();
        }
        set
        {
            if (_setMethod == null)
            {
                RefreshAccessors();
            }
            _setMethod(value);
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public byte[] CompiledRootActivityQualifiedId
    {
        get
        {
            if (_compiledRootActivityQualifiedId == null)
            {
                return _compiledRootActivity.QualifiedId.AsByteArray();
            }

            return _compiledRootActivityQualifiedId;
        }
        set => _compiledRootActivityQualifiedId = value;
    }

    [DataMember(EmitDefaultValue = false)]
    public byte[] ExpressionActivityQualifiedId
    {
        get
        {
            if (_expressionActivityQualifiedId == null)
            {
                return _expressionActivity.QualifiedId.AsByteArray();
            }

            return _expressionActivityQualifiedId;
        }
        set => _expressionActivityQualifiedId = value;
    }

    [DataMember(EmitDefaultValue = false)]
    public List<(string Name, string TypeName)> LocationReferenceCache
    {
        get
        {
            if (_locationReferences == null || _locationReferences.Count == 0)
            {
                return null;
            }
            var durableCache = new List<(string, string)>(_locationReferences.Count);
            foreach (var reference in _locationReferences)
            {
                durableCache.Add((reference.Name, reference.Type.AssemblyQualifiedName));
            }
            return durableCache;
        }
        set
        {
            if (value == null || value.Count == 0)
            {
                _locationReferences = new List<LocationReference>();
                return;
            }
            _locationReferences = new List<LocationReference>(value.Count);
            foreach (var (Name, TypeName) in value)
            {
                _locationReferences.Add(new CompiledLocationReference(Name, Type.GetType(TypeName, throwOnError: true)));
            }
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "expressionId")]
    internal int SerializedExpressionId
    {
        get => _expressionId;
        set => _expressionId = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "locations")]
    internal IList<Location> SerializedLocations
    {
        get => _locations;
        set => _locations = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "rootInstance")]
    internal ActivityInstance SerializedRootInstance
    {
        get => _rootInstance;
        set => _rootInstance = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "expressionText")]
    internal string SerializedExpressionText
    {
        get => _expressionText;
        set => _expressionText = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "forImplementation")]
    internal bool SerializedForImplementation
    {
        get => _forImplementation;
        set => _forImplementation = value;
    }

    private void RefreshAccessors()
    {
        //
        // If we've gotten here is means that we have a location that has roundtripped through persistence
        // CompiledDataContext & ICER don't round trip so we need to get them back from the current tree 
        // and get new pointers to the get/set methods for this expression
        ICompiledExpressionRoot compiledRoot = GetCompiledExpressionRoot();
        CompiledLocation<T> tempLocation = (CompiledLocation<T>)compiledRoot.InvokeExpression(_expressionId, _locations);
        _getMethod = tempLocation._getMethod;
        _setMethod = tempLocation._setMethod;
    }

    private ICompiledExpressionRoot GetCompiledExpressionRoot()
    {
        if (_rootInstance != null && _rootInstance.Activity != null)
        {
            ICompiledExpressionRoot compiledExpressionRoot;
            Activity rootActivity = _rootInstance.Activity;

            if (QualifiedId.TryGetElementFromRoot(rootActivity, _compiledRootActivityQualifiedId, out Activity compiledRootActivity) &&
                QualifiedId.TryGetElementFromRoot(rootActivity, _expressionActivityQualifiedId, out Activity expressionActivity))
            {
                if (CompiledExpressionInvoker.TryGetCompiledExpressionRoot(expressionActivity, compiledRootActivity, out compiledExpressionRoot))
                {
                    //
                    // Revalidate to make sure we didn't hit an ID shift
                    if (compiledExpressionRoot.CanExecuteExpression(_expressionText, true /* this is always a reference */, _locationReferences, out _expressionId))
                    {
                        return compiledExpressionRoot;
                    }
                }
            }
            //
            // We were valid when this location was generated so an ID shift occurred (likely due to a dynamic update)
            // Need to search all of the ICERs for one that can execute this expression.
            if (FindCompiledExpressionRoot(rootActivity, out compiledExpressionRoot))
            {
                return compiledExpressionRoot;
            }
        }
        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnableToLocateCompiledLocationContext(_expressionText)));
    }

    private bool FindCompiledExpressionRoot(Activity activity, out ICompiledExpressionRoot compiledExpressionRoot)
    {
        if (CompiledExpressionInvoker.TryGetCompiledExpressionRoot(activity, _forImplementation, out compiledExpressionRoot))
        {
            if (compiledExpressionRoot.CanExecuteExpression(_expressionText, true /* this is always a reference */, _locationReferences, out _expressionId))
            {
                return true;
            }
        }

        foreach (Activity containedActivity in WorkflowInspectionServices.GetActivities(activity))
        {
            if (FindCompiledExpressionRoot(containedActivity, out compiledExpressionRoot))
            {
                return true;
            }
        }

        compiledExpressionRoot = null;
        return false;
    }

    private class CompiledLocationReference : LocationReference
    {
        private readonly string _name;
        private readonly Type _type;

        public CompiledLocationReference(string name, Type type)
        {
            _name = name;
            _type = type;
        }

        protected override string NameCore => _name;

        protected override Type TypeCore => _type;

        public override Location GetLocation(ActivityContext context)
        {
            //
            // We should never hit this, these references are strictly for preserving location names/types
            // through persistence to allow for revalidation on the other side
            // Actual execution occurs through the locations that were stored separately
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CompiledLocationReferenceGetLocation));
        }
    }
}
