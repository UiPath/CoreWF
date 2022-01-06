// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix,
//    Justification = "Approved Workflow naming")]
[ContentProperty("Delegate")]
public sealed class InvokeDelegate : NativeActivity
{
    private readonly IDictionary<string, Argument> _delegateArguments;
    private bool _hasOutputArguments;

    public InvokeDelegate()
    {
        _delegateArguments = new Dictionary<string, Argument>();
    }

    [DefaultValue(null)]
    public ActivityDelegate Delegate { get; set; }

    public IDictionary<string, Argument> DelegateArguments => _delegateArguments;

    [DefaultValue(null)]
    public Activity Default { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();

        foreach (KeyValuePair<string, Argument> entry in DelegateArguments)
        {
            RuntimeArgument argument = new(entry.Key, entry.Value.ArgumentType, entry.Value.Direction);
            metadata.Bind(entry.Value, argument);
            arguments.Add(argument);
        }

        metadata.SetArgumentsCollection(arguments);
        metadata.AddDelegate(Delegate);

        if (Delegate != null)
        {
            IList<RuntimeDelegateArgument> targetDelegateArguments = Delegate.RuntimeDelegateArguments;
            if (DelegateArguments.Count != targetDelegateArguments.Count)
            {
                metadata.AddValidationError(SR.WrongNumberOfArgumentsForActivityDelegate);
            }

            // Validate that the names and directionality of arguments in DelegateArguments dictionary 
            // match the names and directionality of arguments returned by the ActivityDelegate.GetDelegateParameters 
            // call above. 
            for (int i = 0; i < targetDelegateArguments.Count; i++)
            {
                RuntimeDelegateArgument expectedParameter = targetDelegateArguments[i];
                string parameterName = expectedParameter.Name;
                if (DelegateArguments.TryGetValue(parameterName, out Argument delegateArgument))
                {
                    if (delegateArgument.Direction != expectedParameter.Direction)
                    {
                        metadata.AddValidationError(SR.DelegateParameterDirectionalityMismatch(parameterName, delegateArgument.Direction, expectedParameter.Direction));
                    }

                    if (expectedParameter.Direction == ArgumentDirection.In)
                    {
                        if (!TypeHelper.AreTypesCompatible(delegateArgument.ArgumentType, expectedParameter.Type))
                        {
                            metadata.AddValidationError(SR.DelegateInArgumentTypeMismatch(parameterName, expectedParameter.Type, delegateArgument.ArgumentType));
                        }
                    }
                    else
                    {
                        if (!TypeHelper.AreTypesCompatible(expectedParameter.Type, delegateArgument.ArgumentType))
                        {
                            metadata.AddValidationError(SR.DelegateOutArgumentTypeMismatch(parameterName, expectedParameter.Type, delegateArgument.ArgumentType));
                        }
                    }
                }
                else
                {
                    metadata.AddValidationError(SR.InputParametersMissing(expectedParameter.Name));
                }

                if (!_hasOutputArguments && ArgumentDirectionHelper.IsOut(expectedParameter.Direction))
                {
                    _hasOutputArguments = true;
                }
            }
        }

        metadata.AddChild(Default);
    }

    protected override void Execute(NativeActivityContext context)
    {
        if (Delegate == null || Delegate.Handler == null)
        {
            if (Default != null)
            {
                context.ScheduleActivity(Default);
            }

            return;
        }

        Dictionary<string, object> inputParameters = new();

        if (DelegateArguments.Count > 0)
        {
            foreach (KeyValuePair<string, Argument> entry in DelegateArguments)
            {
                if (ArgumentDirectionHelper.IsIn(entry.Value.Direction))
                {
                    inputParameters.Add(entry.Key, entry.Value.Get(context));
                }
            }
        }

        context.ScheduleDelegate(Delegate, inputParameters, new DelegateCompletionCallback(OnHandlerComplete), null);
    }

    private void OnHandlerComplete(NativeActivityContext context, ActivityInstance completedInstance, IDictionary<string, object> outArguments)
    {
        if (_hasOutputArguments)
        {
            foreach (KeyValuePair<string, object> entry in outArguments)
            {
                if (DelegateArguments.TryGetValue(entry.Key, out Argument argument))
                {
                    if (ArgumentDirectionHelper.IsOut(argument.Direction))
                    {
                        DelegateArguments[entry.Key].Set(context, entry.Value);
                    }
                    else
                    {
                        Fx.Assert(string.Format(CultureInfo.InvariantCulture, "Expected argument named '{0}' in the DelegateArguments collection to be an out argument.", entry.Key));
                    }
                }
            }
        }
    }
}
