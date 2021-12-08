// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Activities;
using Internals;
using Runtime;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldHaveCorrectSuffix)]
[Fx.Tag.XamlVisible(false)]
public sealed class ExecutionProperties : IEnumerable<KeyValuePair<string, object>>
{
    private static IEnumerable<KeyValuePair<string, object>> emptyKeyValues;
    private readonly ActivityContext _context;
    private readonly ActivityInstance _scope;
    private ExecutionPropertyManager _properties;
    private readonly IdSpace _currentIdSpace;

    internal ExecutionProperties(ActivityContext currentContext, ActivityInstance scope, ExecutionPropertyManager properties)
    {
        _context = currentContext;
        _scope = scope;
        _properties = properties;

        if (_context != null)
        {
            _currentIdSpace = _context.Activity.MemberOf;
        }
    }

    public bool IsEmpty => _properties == null;

    private static IEnumerable<KeyValuePair<string, object>> EmptyKeyValues
    {
        get
        {
            emptyKeyValues ??= Array.Empty<KeyValuePair<string, object>>();
            return emptyKeyValues;
        }
    }

    [Fx.Tag.InheritThrows(From = "Register", FromDeclaringType = typeof(IPropertyRegistrationCallback))]
    public void Add(string name, object property) => Add(name, property, false, false);

    [Fx.Tag.InheritThrows(From = "Add")]
    public void Add(string name, object property, bool onlyVisibleToPublicChildren) => Add(name, property, false, onlyVisibleToPublicChildren);

    internal void Add(string name, object property, bool skipValidations, bool onlyVisibleToPublicChildren)
    {
        if (!skipValidations)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
            }

            if (property == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(property));
            }

            ThrowIfActivityExecutionContextDisposed();
            ThrowIfChildrenAreExecuting();
        }

        _properties?.ThrowIfAlreadyDefined(name, _scope);

        if (property is IPropertyRegistrationCallback registrationCallback)
        {
            registrationCallback.Register(new RegistrationContext(_properties, _currentIdSpace));
        }

        if (_properties == null)
        {
            _properties = new ExecutionPropertyManager(_scope);
        }
        else if (!_properties.IsOwner(_scope))
        {
            // TODO, 51474, Thread properties are broken when the scope is not the current activity.  This will only happen for NoPersistProperty right now so it doesn't matter.
            _properties = new ExecutionPropertyManager(_scope, _properties);
        }

        IdSpace visibility = null;

        if (onlyVisibleToPublicChildren)
        {
            Fx.Assert(_currentIdSpace != null, "We should never call OnlyVisibleToPublicChildren when we don't have a currentIdSpace");
            visibility = _currentIdSpace;
        }

        _properties.Add(name, property, visibility);
    }

    [Fx.Tag.InheritThrows(From = "Unregister", FromDeclaringType = typeof(IPropertyRegistrationCallback))]
    public bool Remove(string name) => Remove(name, false);

    internal bool Remove(string name, bool skipValidations)
    {
        if (!skipValidations)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
            }

            ThrowIfActivityExecutionContextDisposed();
        }

        if (_properties != null && _properties.IsOwner(_scope))
        {
            object property = _properties.GetPropertyAtCurrentScope(name);

            if (property != null)
            {
                if (!skipValidations)
                {

                    if (property is not Handle handleProperty || !handleProperty.CanBeRemovedWithExecutingChildren)
                    {
                        ThrowIfChildrenAreExecuting();
                    }
                }

                _properties.Remove(name);


                if (property is IPropertyRegistrationCallback registrationCallback)
                {
                    registrationCallback.Unregister(new RegistrationContext(_properties, _currentIdSpace));
                }

                return true;
            }
        }

        return false;
    }

    public object Find(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        return _properties?.GetProperty(name, _currentIdSpace);
    }

    // Note that we don't need to pass the IdSpace here because we're
    // just checking for things that this activity has added.
    internal object FindAtCurrentScope(string name)
    {
        Fx.Assert(!string.IsNullOrEmpty(name), "We should only call this with non-null names");

        return _properties == null || !_properties.IsOwner(_scope) ? null : _properties.GetPropertyAtCurrentScope(name);
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => GetKeyValues().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetKeyValues().GetEnumerator();

    private IEnumerable<KeyValuePair<string, object>> GetKeyValues()
        => _properties != null ? _properties.GetFlattenedProperties(_currentIdSpace) : EmptyKeyValues;

    private void ThrowIfChildrenAreExecuting()
    {
        if (_scope.HasChildren)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotAddOrRemoveWithChildren));
        }
    }

    private void ThrowIfActivityExecutionContextDisposed()
    {
        if (_context.IsDisposed)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AECForPropertiesHasBeenDisposed));
        }
    }
}
