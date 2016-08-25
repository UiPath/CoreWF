// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.CoreWf.Statements
{
    public sealed class Confirm : NativeActivity
    {
        private static Constraint s_confirmWithNoTarget = Confirm.ConfirmWithNoTarget();

        private InternalConfirm _internalConfirm;
        private DefaultConfirmation _defaultConfirmation;

        private Variable<CompensationToken> _currentCompensationToken;

        public Confirm()
            : base()
        {
            _currentCompensationToken = new Variable<CompensationToken>();
        }

        public InArgument<CompensationToken> Target
        {
            get;
            set;
        }

        private DefaultConfirmation DefaultConfirmation
        {
            get
            {
                if (_defaultConfirmation == null)
                {
                    _defaultConfirmation = new DefaultConfirmation()
                    {
                        Target = new InArgument<CompensationToken>(_currentCompensationToken),
                    };
                }

                return _defaultConfirmation;
            }
        }

        private InternalConfirm InternalConfirm
        {
            get
            {
                if (_internalConfirm == null)
                {
                    _internalConfirm = new InternalConfirm()
                    {
                        Target = new InArgument<CompensationToken>(new ArgumentValue<CompensationToken> { ArgumentName = "Target" }),
                    };
                }

                return _internalConfirm;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument targetArgument = new RuntimeArgument("Target", typeof(CompensationToken), ArgumentDirection.In);
            metadata.Bind(this.Target, targetArgument);

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
            return new List<Constraint>(1) { s_confirmWithNoTarget };
        }

        private static Constraint ConfirmWithNoTarget()
        {
            DelegateInArgument<Confirm> element = new DelegateInArgument<Confirm> { Name = "element" };
            DelegateInArgument<ValidationContext> validationContext = new DelegateInArgument<ValidationContext> { Name = "validationContext" };
            Variable<bool> assertFlag = new Variable<bool> { Name = "assertFlag" };
            Variable<IEnumerable<Activity>> elements = new Variable<IEnumerable<Activity>>() { Name = "elements" };
            Variable<int> index = new Variable<int>() { Name = "index" };

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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ConfirmWithoutCompensableActivity(this.DisplayName)));
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
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidConfirmActivityUsage(this.DisplayName)));
                }
            }
            else
            {
                CompensationToken compensationToken = Target.Get(context);
                CompensationTokenData tokenData = compensationToken == null ? null : compensationExtension.Get(compensationToken.CompensationId);

                if (compensationToken == null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("Target", SR.InvalidCompensationToken(this.DisplayName));
                }

                if (compensationToken.ConfirmCalled)
                {
                    // No-Op
                    return;
                }

                if (tokenData == null || tokenData.CompensationState != CompensationState.Completed)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CompensableActivityAlreadyConfirmedOrCompensated));
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
}
