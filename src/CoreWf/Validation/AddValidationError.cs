// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Validation;

public sealed class AddValidationError : NativeActivity
{
    public AddValidationError() { }

    public InArgument<string> Message { get; set; }

    [DefaultValue(null)]
    public InArgument<bool> IsWarning { get; set; }

    [DefaultValue(null)]
    public InArgument<string> PropertyName { get; set; }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();

        RuntimeArgument messageArgument = new("Message", typeof(string), ArgumentDirection.In);
        metadata.Bind(Message, messageArgument);
        arguments.Add(messageArgument);

        RuntimeArgument isWarningArgument = new("IsWarning", typeof(bool), ArgumentDirection.In, false);
        metadata.Bind(IsWarning, isWarningArgument);
        arguments.Add(isWarningArgument);
            
        RuntimeArgument propertyNameArgument = new("PropertyName", typeof(string), ArgumentDirection.In, false);
        metadata.Bind(PropertyName, propertyNameArgument);
        arguments.Add(propertyNameArgument);

        metadata.SetArgumentsCollection(arguments);
    }

    protected override void Execute(NativeActivityContext context)
    {
        bool isWarning = false;
        string propertyName = string.Empty;

        if (IsWarning != null)
        {
            isWarning = IsWarning.Get(context);
        }
            
        if (PropertyName != null)
        {
            propertyName = PropertyName.Get(context);            
        }
            
        Constraint.AddValidationError(context, new ValidationError(Message.Get(context), isWarning, propertyName));
    }
}
