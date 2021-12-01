// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class NoPersistScope : NativeActivity
{
    private static Constraint constraint;
    private readonly Variable<NoPersistHandle> _noPersistHandle;

    public NoPersistScope()
    {
        _noPersistHandle = new Variable<NoPersistHandle>();
        Constraints.Add(Constraint);
    }

    [DefaultValue(null)]
    public Activity Body { get; set; }

    private static Constraint Constraint
    {
        get
        {
            constraint ??= NoPersistInScope();
            return constraint;
        }
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.AddChild(Body);
        metadata.AddImplementationVariable(_noPersistHandle);
    }

    protected override void Execute(NativeActivityContext context)
    {
        if (Body != null)
        {
            NoPersistHandle handle = _noPersistHandle.Get(context);
            handle.Enter(context);
            context.ScheduleActivity(Body);
        }
    }

    private static Constraint NoPersistInScope()
    {
        DelegateInArgument<ValidationContext> validationContext = new("validationContext");
        DelegateInArgument<NoPersistScope> noPersistScope = new("noPersistScope");
        Variable<bool> isConstraintSatisfied = new("isConstraintSatisfied", true);
        Variable<IEnumerable<Activity>> childActivities = new("childActivities");
        Variable<string> constraintViolationMessage = new("constraintViolationMessage");

        return new Constraint<NoPersistScope>
        {
            Body = new ActivityAction<NoPersistScope, ValidationContext>
            {
                Argument1 = noPersistScope,
                Argument2 = validationContext,
                Handler = new Sequence
                {
                    Variables =
                    {
                        isConstraintSatisfied,
                        childActivities,
                        constraintViolationMessage,
                    },
                    Activities =
                    {
                        new Assign<IEnumerable<Activity>>
                        {
                            To = childActivities,
                            Value = new GetChildSubtree
                            {
                                ValidationContext = validationContext,
                            },
                        },
                        new Assign<bool>
                        {
                            To = isConstraintSatisfied,
                            Value = new CheckNoPersistInDescendants
                            {
                                NoPersistScope = noPersistScope,
                                DescendantActivities = childActivities,
                                ConstraintViolationMessage = constraintViolationMessage,
                            },
                        },
                        new AssertValidation
                        {
                            Assertion = isConstraintSatisfied,
                            Message = constraintViolationMessage,
                        },
                    }
                }
            }
        };
    }

    private sealed class CheckNoPersistInDescendants : CodeActivity<bool>
    {
        [RequiredArgument]
        public InArgument<NoPersistScope> NoPersistScope { get; set; }

        [RequiredArgument]
        public InArgument<IEnumerable<Activity>> DescendantActivities { get; set; }

        [RequiredArgument]
        public OutArgument<string> ConstraintViolationMessage { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            Collection<RuntimeArgument> runtimeArguments = new();

            RuntimeArgument noPersistScopeArgument = new("NoPersistScope", typeof(NoPersistScope), ArgumentDirection.In);
            metadata.Bind(NoPersistScope, noPersistScopeArgument);
            runtimeArguments.Add(noPersistScopeArgument);

            RuntimeArgument descendantActivitiesArgument = new("DescendantActivities", typeof(IEnumerable<Activity>), ArgumentDirection.In);
            metadata.Bind(DescendantActivities, descendantActivitiesArgument);
            runtimeArguments.Add(descendantActivitiesArgument);

            RuntimeArgument constraintViolationMessageArgument = new("ConstraintViolationMessage", typeof(string), ArgumentDirection.Out);
            metadata.Bind(ConstraintViolationMessage, constraintViolationMessageArgument);
            runtimeArguments.Add(constraintViolationMessageArgument);

            RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
            metadata.Bind(Result, resultArgument);
            runtimeArguments.Add(resultArgument);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override bool Execute(CodeActivityContext context)
        {
            IEnumerable<Activity> descendantActivities = DescendantActivities.Get(context);
            Fx.Assert(descendantActivities != null, "this.DescendantActivities cannot evaluate to null.");

            Persist firstPersist = descendantActivities.OfType<Persist>().FirstOrDefault();
            if (firstPersist != null)
            {
                NoPersistScope noPersistScope = NoPersistScope.Get(context);
                Fx.Assert(noPersistScope != null, "this.NoPersistScope cannot evaluate to null.");

                string constraintViolationMessage = SR.NoPersistScopeCannotContainPersist(noPersistScope.DisplayName, firstPersist.DisplayName);
                ConstraintViolationMessage.Set(context, constraintViolationMessage);
                return false;
            }

            return true;
        }
    }
}
