// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities.Statements;

public sealed class Confirm : NativeActivity
{
    private static readonly Constraint confirmWithNoTarget = Confirm.ConfirmWithNoTarget();
    private InternalConfirm _internalConfirm;
    private DefaultConfirmation _defaultConfirmation;
    private readonly Variable<CompensationToken> _currentCompensationToken;

    public Confirm()
        : base()
    {
        _currentCompensationToken = new Variable<CompensationToken>();
    }

    public InArgument<CompensationToken> Target { get; set; }

    private DefaultConfirmation DefaultConfirmation
    {
        get
        {
            _defaultConfirmation ??= new DefaultConfirmation()
                {
                    Target = new InArgument<CompensationToken>(_currentCompensationToken),
                };
            return _defaultConfirmation;
        }
    }

    private InternalConfirm InternalConfirm
    {
        get
        {
            _internalConfirm ??= new InternalConfirm()
                {
                    Target = new InArgument<CompensationToken>(new ArgumentValue<CompensationToken> { ArgumentName = "Target" }),
                };
            return _internalConfirm;
        }
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument targetArgument = new("Target", typeof(CompensationToken), ArgumentDirection.In);
        metadata.Bind(Target, targetArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                targetArgument
            });

        metadata.SetImplementationVariablesCollection(
            new Collection<Variable>
            {
                _currentCompensationToken
            });

        Fx.Assert(DefaultConfirmation != null, "DefaultConfirmation must be valid");
        Fx.Assert(InternalConfirm != null, "InternalConfirm must be valid");
        metadata.SetImplementationChildrenCollection(
            new Collection<Activity>
            {
                DefaultConfirmation, 
                InternalConfirm
            });
    }

    internal override IList<Constraint> InternalGetConstraints()
    {
        return new List<Constraint>(1) { confirmWithNoTarget };
    }

    private static Constraint ConfirmWithNoTarget()
    {
        DelegateInArgument<Confirm> element = new() { Name = "element" };
        DelegateInArgument<ValidationContext> validationContext = new() { Name = "validationContext" };
        Variable<bool> assertFlag = new() { Name = "assertFlag" };
        Variable<IEnumerable<Activity>> elements = new() { Name = "elements" };
        Variable<int> index = new() { Name = "index" };

        return new Constraint<Confirm>
        {
            Body = new ActivityAction<Confirm, ValidationContext>
            {
                Argument1 = element,
                Argument2 = validationContext,
                Handler = new Sequence
                {
                    Variables = 
                    {
                        assertFlag,
                        elements,
                        index
                    },
                    Activities =
                    {
                        new If
                        {
                            Condition = new InArgument<bool>((env) => element.Get(env).Target != null),
                            Then = new Assign<bool>
                            {
                                To = assertFlag,
                                Value = true
                            },
                            Else = new Sequence
                            {
                                Activities = 
                                {
                                    new Assign<IEnumerable<Activity>>
                                    {
                                        To = elements,
                                        Value = new GetParentChain
                                        {
                                            ValidationContext = validationContext,
                                        },
                                    },
                                    new While(env => (assertFlag.Get(env) != true) &&
                                        index.Get(env) < elements.Get(env).Count())
                                    {
                                        Body = new Sequence
                                        {
                                            Activities = 
                                            {
                                                new If(env => (elements.Get(env).ElementAt(index.Get(env))).GetType() == typeof(CompensationParticipant))
                                                {
                                                    Then = new Assign<bool>
                                                    {
                                                        To = assertFlag,
                                                        Value = true                                                            
                                                    },
                                                },
                                                new Assign<int>
                                                {
                                                    To = index,
                                                    Value = new InArgument<int>(env => index.Get(env) + 1)
                                                },
                                            }
                                        }
                                    }
                                }
                            }                                
                        },
                        new AssertValidation
                        {
                            Assertion = new InArgument<bool>(assertFlag),
                            Message = new InArgument<string>(SR.ConfirmWithNoTargetConstraint)   
                        }
                    }
                }
            }
        };
    }

    protected override void Execute(NativeActivityContext context)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        if (compensationExtension == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ConfirmWithoutCompensableActivity(DisplayName)));
        }

        if (Target.IsEmpty)
        {
            CompensationToken ambientCompensationToken = (CompensationToken)context.Properties.Find(CompensationToken.PropertyName);
            CompensationTokenData ambientTokenData = ambientCompensationToken == null ? null : compensationExtension.Get(ambientCompensationToken.CompensationId);

            if (ambientTokenData != null && ambientTokenData.IsTokenValidInSecondaryRoot)
            {
                _currentCompensationToken.Set(context, ambientCompensationToken);
                if (ambientTokenData.ExecutionTracker.Count > 0)
                {
                    context.ScheduleActivity(DefaultConfirmation);
                }
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidConfirmActivityUsage(DisplayName)));
            }
        }
        else
        {
            CompensationToken compensationToken = Target.Get(context);
            CompensationTokenData tokenData = compensationToken == null ? null : compensationExtension.Get(compensationToken.CompensationId);

            if (compensationToken == null)
            {
                throw FxTrace.Exception.Argument("Target", SR.InvalidCompensationToken(DisplayName));
            }

            if (compensationToken.ConfirmCalled)
            {
                // No-Op
                return;
            }

            if (tokenData == null || tokenData.CompensationState != CompensationState.Completed)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CompensableActivityAlreadyConfirmedOrCompensated));
            }

            // A valid in-arg was passed...     
            tokenData.CompensationState = CompensationState.Confirming;
            compensationToken.ConfirmCalled = true;
            context.ScheduleActivity(InternalConfirm);
        }
    }

    protected override void Cancel(NativeActivityContext context)
    {
        // Suppress Cancel   
    }
}
