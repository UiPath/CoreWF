// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Expressions;

public sealed class ArgumentValue<T> : EnvironmentLocationValue<T>
{
    private RuntimeArgument _targetArgument;

    public ArgumentValue() { }

    public ArgumentValue(string argumentName)
    {
        ArgumentName = argumentName;
    }

    public string ArgumentName { get; set; }

    public override LocationReference LocationReference => _targetArgument;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _targetArgument = null;

        if (string.IsNullOrEmpty(ArgumentName))
        {
            metadata.AddValidationError(SR.ArgumentNameRequired);
        }
        else
        {
            _targetArgument = ActivityUtilities.FindArgument(ArgumentName, this);

            if (_targetArgument == null)
            {
                metadata.AddValidationError(SR.ArgumentNotFound(ArgumentName));
            }
            else if (!TypeHelper.AreTypesCompatible(_targetArgument.Type, typeof(T)))
            {
                metadata.AddValidationError(SR.ArgumentTypeMustBeCompatible(ArgumentName, _targetArgument.Type, typeof(T)));
            }
        }
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ArgumentName))
        {
            return ArgumentName;
        }

        return base.ToString();
    }
}
