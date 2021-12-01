// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class WorkflowDataContext : CustomTypeDescriptor, INotifyPropertyChanged, IDisposable
{
    private readonly ActivityExecutor _executor;
    private ActivityInstance _activityInstance;
    private IDictionary<Location, PropertyDescriptorImpl> _locationMapping;
    private PropertyChangedEventHandler _propertyChangedEventHandler;
    private readonly PropertyDescriptorCollection _properties;
    private ActivityContext _cachedResolutionContext;

    internal WorkflowDataContext(ActivityExecutor executor, ActivityInstance activityInstance, bool includeLocalVariables)
    {
        _executor = executor;
        _activityInstance = activityInstance;
        IncludesLocalVariables = includeLocalVariables;
        _properties = CreateProperties();
    }

    internal bool IncludesLocalVariables { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    // We want our own cached ActivityContext rather than using this.executor.GetResolutionContext
    // because there is no synchronization of access to the executor's cached object and access thru
    // this WorkflowDataContext will not be done on the workflow runtime thread.
    private ActivityContext ResolutionContext
    {
        get
        {
            ThrowIfEnvironmentDisposed();
            if (_cachedResolutionContext == null)
            {
                _cachedResolutionContext = new ActivityContext(_activityInstance, _executor)
                {
                    AllowChainedEnvironmentAccess = true
                };
            }
            else
            {
                _cachedResolutionContext.Reinitialize(_activityInstance, _executor);
            }
            return _cachedResolutionContext;
        }
    }

    private PropertyChangedEventHandler PropertyChangedEventHandler
    {
        get
        {
            _propertyChangedEventHandler ??= new PropertyChangedEventHandler(OnLocationChanged);
            return _propertyChangedEventHandler;
        }
    }

    private PropertyDescriptorCollection CreateProperties()
    {
        // The name in child Activity will shadow the name in parent.
        Dictionary<string, object> names = new();

        List<PropertyDescriptorImpl> propertyList = new();

        LocationReferenceEnvironment environment = _activityInstance.Activity.PublicEnvironment;
        bool isLocalEnvironment = true;
        while (environment != null)
        {
            foreach (LocationReference locRef in environment.GetLocationReferences())
            {
                if (IncludesLocalVariables || !isLocalEnvironment || locRef is not Variable)
                {
                    AddProperty(locRef, names, propertyList);
                }
            }

            environment = environment.Parent;
            isLocalEnvironment = false;
        }

        return new PropertyDescriptorCollection(propertyList.ToArray(), true);
    }

    private void AddProperty(LocationReference reference, Dictionary<string, object> names,
        List<PropertyDescriptorImpl> propertyList)
    {
        if (!string.IsNullOrEmpty(reference.Name) &&
            !names.ContainsKey(reference.Name))
        {
            names.Add(reference.Name, reference);
            PropertyDescriptorImpl property = new(reference);
            propertyList.Add(property);
            AddNotifyHandler(property);
        }
    }

    private void AddNotifyHandler(PropertyDescriptorImpl property)
    {
        ActivityContext activityContext = ResolutionContext;
        try
        {
            Location location = property.LocationReference.GetLocation(activityContext);
            if (location is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += PropertyChangedEventHandler;

                _locationMapping ??= new Dictionary<Location, PropertyDescriptorImpl>();
                _locationMapping.Add(location, property);
            }
        }
        finally
        {
            activityContext.Dispose();
        }
    }

    private void OnLocationChanged(object sender, PropertyChangedEventArgs e)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
        {
            Location location = (Location)sender;

            Fx.Assert(_locationMapping != null, "Location mapping must not be null.");
            if (_locationMapping.TryGetValue(location, out PropertyDescriptorImpl property))
            {
                if (e.PropertyName == "Value")
                {
                    handler(this, new PropertyChangedEventArgs(property.Name));
                }
                else
                {
                    handler(this, new PropertyChangedEventArgs(property.Name + "." + e.PropertyName));
                }
            }
        }
    }

    public void Dispose()
    {
        if (_locationMapping != null)
        {
            foreach (KeyValuePair<Location, PropertyDescriptorImpl> pair in _locationMapping)
            {
                if (pair.Key is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= PropertyChangedEventHandler;
                }
            }
        }
    }

    // We need a separate method here from Dispose(), because Dispose currently
    // doesn't make the WDC uncallable, it just unhooks it from notifications.
    internal void DisposeEnvironment() => _activityInstance = null;

    private void ThrowIfEnvironmentDisposed()
    {
        if (_activityInstance == null)
        {
            throw FxTrace.Exception.AsError(
                new ObjectDisposedException(GetType().FullName, SR.WDCDisposed));
        }
    }

    public override PropertyDescriptorCollection GetProperties() => _properties;

    private class PropertyDescriptorImpl : PropertyDescriptor
    {
        private readonly LocationReference reference;
        // TODO: We should support readonly LocationReferences.
        // bool isReadOnly;

        public PropertyDescriptorImpl(LocationReference reference)
            : base(reference.Name, Array.Empty<Attribute>())
        {
            this.reference = reference;
        }

        public override Type ComponentType => typeof(WorkflowDataContext);

        public override bool IsReadOnly
        {
            get
            {
                // TODO: We should support readonly LocationReferences.
                // return this.isReadOnly;
                return false;
            }
        }

        public override Type PropertyType => reference.Type;

        public LocationReference LocationReference => reference;

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component)
        {
            WorkflowDataContext dataContext = (WorkflowDataContext)component;

            ActivityContext activityContext = dataContext.ResolutionContext;
            try
            {
                return reference.GetLocation(activityContext).Value;
            }
            finally
            {
                activityContext.Dispose();
            }
        }

        public override void ResetValue(object component) => throw FxTrace.Exception.AsError(new NotSupportedException(SR.CannotResetPropertyInDataContext));

        public override void SetValue(object component, object value)
        {
            if (IsReadOnly)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.PropertyReadOnlyInWorkflowDataContext(Name)));
            }

            WorkflowDataContext dataContext = (WorkflowDataContext)component;

            ActivityContext activityContext = dataContext.ResolutionContext;
            try
            {
                Location location = reference.GetLocation(activityContext);
                location.Value = value;
            }
            finally
            {
                activityContext.Dispose();
            }
        }

        public override bool ShouldSerializeValue(object component) => true;
    }
}
