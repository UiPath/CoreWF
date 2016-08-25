// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Validation;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Microsoft.CoreWf
{
    public struct CodeActivityMetadata
    {
        private Activity _activity;
        private LocationReferenceEnvironment _environment;
        private bool _createEmptyBindings;

        internal CodeActivityMetadata(Activity activity, LocationReferenceEnvironment environment, bool createEmptyBindings)
        {
            _activity = activity;
            _environment = environment;
            _createEmptyBindings = createEmptyBindings;
        }

        internal bool CreateEmptyBindings
        {
            get
            {
                return _createEmptyBindings;
            }
        }

        public LocationReferenceEnvironment Environment
        {
            get
            {
                return _environment;
            }
        }

        internal Activity CurrentActivity
        {
            get
            {
                return _activity;
            }
        }

        public bool HasViolations
        {
            get
            {
                if (_activity == null)
                {
                    return false;
                }
                else
                {
                    return _activity.HasTempViolations;
                }
            }
        }

        public static bool operator ==(CodeActivityMetadata left, CodeActivityMetadata right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CodeActivityMetadata left, CodeActivityMetadata right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CodeActivityMetadata))
            {
                return false;
            }

            CodeActivityMetadata other = (CodeActivityMetadata)obj;
            return other._activity == _activity && other.Environment == this.Environment
                && other.CreateEmptyBindings == this.CreateEmptyBindings;
        }

        public override int GetHashCode()
        {
            if (_activity == null)
            {
                return 0;
            }
            else
            {
                return _activity.GetHashCode();
            }
        }

        public void Bind(Argument binding, RuntimeArgument argument)
        {
            ThrowIfDisposed();

            Argument.TryBind(binding, argument, _activity);
        }

        public void SetValidationErrorsCollection(Collection<ValidationError> validationErrors)
        {
            ThrowIfDisposed();

            ActivityUtilities.RemoveNulls(validationErrors);

            _activity.SetTempValidationErrorCollection(validationErrors);
        }

        public void AddValidationError(string validationErrorMessage)
        {
            AddValidationError(new ValidationError(validationErrorMessage));
        }

        public void AddValidationError(ValidationError validationError)
        {
            ThrowIfDisposed();

            if (validationError != null)
            {
                _activity.AddTempValidationError(validationError);
            }
        }

        public void SetArgumentsCollection(Collection<RuntimeArgument> arguments)
        {
            ThrowIfDisposed();

            ActivityUtilities.RemoveNulls(arguments);

            _activity.SetArgumentsCollection(arguments, _createEmptyBindings);
        }

        public void AddArgument(RuntimeArgument argument)
        {
            ThrowIfDisposed();

            if (argument != null)
            {
                _activity.AddArgument(argument, _createEmptyBindings);
            }
        }

        //public Collection<RuntimeArgument> GetArgumentsWithReflection()
        //{
        //    return Activity.ReflectedInformation.GetArguments(this.activity);
        //}

        public void AddDefaultExtensionProvider<T>(Func<T> extensionProvider)
            where T : class
        {
            if (extensionProvider == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("extensionProvider");
            }
            _activity.AddDefaultExtensionProvider(extensionProvider);
        }

        public void RequireExtension<T>()
            where T : class
        {
            _activity.RequireExtension(typeof(T));
        }

        public void RequireExtension(Type extensionType)
        {
            if (extensionType == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("extensionType");
            }
            if (extensionType.GetTypeInfo().IsValueType)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("extensionType", SR.RequireExtensionOnlyAcceptsReferenceTypes(extensionType.FullName));
            }
            _activity.RequireExtension(extensionType);
        }

        internal void ThrowIfDisposed()
        {
            if (_activity == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new ObjectDisposedException(ToString()));
            }
        }

        internal void Dispose()
        {
            _activity = null;
        }
    }
}
