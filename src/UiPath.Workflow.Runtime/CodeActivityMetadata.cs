// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;
using Internals;
using Validation;

public struct CodeActivityMetadata
{
    private Activity _activity;
    private readonly LocationReferenceEnvironment _environment;
    private readonly bool _createEmptyBindings;

    internal CodeActivityMetadata(Activity activity, LocationReferenceEnvironment environment, bool createEmptyBindings)
    {
        _activity = activity;
        _environment = environment;
        _createEmptyBindings = createEmptyBindings;
    }

    internal bool CreateEmptyBindings => _createEmptyBindings;

    public LocationReferenceEnvironment Environment => _environment;

    internal Activity CurrentActivity => _activity;

    public bool HasViolations => _activity != null && _activity.HasTempViolations;

    public static bool operator ==(CodeActivityMetadata left, CodeActivityMetadata right) => left.Equals(right);

    public static bool operator !=(CodeActivityMetadata left, CodeActivityMetadata right) => !left.Equals(right);

    public override bool Equals(object obj)
    {
        if (obj is not CodeActivityMetadata)
        {
            return false;
        }

        CodeActivityMetadata other = (CodeActivityMetadata)obj;
        return other._activity == _activity && other.Environment == Environment
            && other.CreateEmptyBindings == CreateEmptyBindings;
    }

    public override int GetHashCode() => _activity == null ? 0 : _activity.GetHashCode();

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

    public void AddValidationError(string validationErrorMessage) => AddValidationError(new ValidationError(validationErrorMessage));

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

    public Collection<RuntimeArgument> GetArgumentsWithReflection() => Activity.ReflectedInformation.GetArguments(_activity);

    public void AddDefaultExtensionProvider<T>(Func<T> extensionProvider)
        where T : class
    {
        if (extensionProvider == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(extensionProvider));
        }
        _activity.AddDefaultExtensionProvider(extensionProvider);
    }

    public void RequireExtension<T>()
        where T : class => _activity.RequireExtension(typeof(T));

    public void RequireExtension(Type extensionType)
    {
        if (extensionType == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(extensionType));
        }
        if (extensionType.IsValueType)
        {
            throw FxTrace.Exception.Argument(nameof(extensionType), SR.RequireExtensionOnlyAcceptsReferenceTypes(extensionType.FullName));
        }
        _activity.RequireExtension(extensionType);
    }

    internal void ThrowIfDisposed()
    {
        if (_activity == null)
        {
            throw FxTrace.Exception.AsError(new ObjectDisposedException(ToString()));
        }
    }

    internal void Dispose() => _activity = null;
}
