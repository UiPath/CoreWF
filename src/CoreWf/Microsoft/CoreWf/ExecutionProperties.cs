// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class ExecutionProperties : IEnumerable<KeyValuePair<string, object>>
    {
        private static IEnumerable<KeyValuePair<string, object>> s_emptyKeyValues;

        private ActivityContext _context;
        private ActivityInstance _scope;
        private ExecutionPropertyManager _properties;
        private IdSpace _currentIdSpace;

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

        public bool IsEmpty
        {
            get
            {
                return (_properties == null);
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> EmptyKeyValues
        {
            get
            {
                if (s_emptyKeyValues == null)
                {
                    s_emptyKeyValues = new KeyValuePair<string, object>[0];
                }
                return s_emptyKeyValues;
            }
        }

        [Fx.Tag.InheritThrows(From = "Register", FromDeclaringType = typeof(IPropertyRegistrationCallback))]
        public void Add(string name, object property)
        {
            Add(name, property, false, false);
        }

        [Fx.Tag.InheritThrows(From = "Add")]
        public void Add(string name, object property, bool onlyVisibleToPublicChildren)
        {
            Add(name, property, false, onlyVisibleToPublicChildren);
        }

        internal void Add(string name, object property, bool skipValidations, bool onlyVisibleToPublicChildren)
        {
            if (!skipValidations)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
                }

                if (property == null)
                {
                    throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("property");
                }

                ThrowIfActivityExecutionContextDisposed();
                ThrowIfChildrenAreExecuting();
            }

            if (_properties != null)
            {
                _properties.ThrowIfAlreadyDefined(name, _scope);
            }

            IPropertyRegistrationCallback registrationCallback = property as IPropertyRegistrationCallback;

            if (registrationCallback != null)
            {
                registrationCallback.Register(new RegistrationContext(_properties, _currentIdSpace));
            }

            if (_properties == null)
            {
                _properties = new ExecutionPropertyManager(_scope);
            }
            else if (!_properties.IsOwner(_scope))
            {
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
        public bool Remove(string name)
        {
            return Remove(name, false);
        }

        internal bool Remove(string name, bool skipValidations)
        {
            if (!skipValidations)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
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
                        Handle handleProperty = property as Handle;

                        if (handleProperty == null || !handleProperty.CanBeRemovedWithExecutingChildren)
                        {
                            ThrowIfChildrenAreExecuting();
                        }
                    }

                    _properties.Remove(name);

                    IPropertyRegistrationCallback registrationCallback = property as IPropertyRegistrationCallback;

                    if (registrationCallback != null)
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
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
            }

            if (_properties == null)
            {
                return null;
            }
            else
            {
                return _properties.GetProperty(name, _currentIdSpace);
            }
        }

        // Note that we don't need to pass the IdSpace here because we're
        // just checking for things that this activity has added.
        internal object FindAtCurrentScope(string name)
        {
            Fx.Assert(!string.IsNullOrEmpty(name), "We should only call this with non-null names");

            if (_properties == null || !_properties.IsOwner(_scope))
            {
                return null;
            }
            else
            {
                return _properties.GetPropertyAtCurrentScope(name);
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return GetKeyValues().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetKeyValues().GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> GetKeyValues()
        {
            if (_properties != null)
            {
                return _properties.GetFlattenedProperties(_currentIdSpace);
            }
            else
            {
                return EmptyKeyValues;
            }
        }

        private void ThrowIfChildrenAreExecuting()
        {
            if (_scope.HasChildren)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotAddOrRemoveWithChildren));
            }
        }

        private void ThrowIfActivityExecutionContextDisposed()
        {
            if (_context.IsDisposed)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.AECForPropertiesHasBeenDisposed));
            }
        }
    }
}


