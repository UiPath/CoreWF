// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class CompensableActivity : NativeActivity<CompensationToken>
{
    private static readonly Constraint noCompensableActivityInSecondaryRoot = NoCompensableActivityInSecondaryRoot();
    private Collection<Variable> _variables;
    private CompensationParticipant _compensationParticipant;
    private readonly Variable<long> _currentCompensationId;
    private readonly Variable<CompensationToken> _currentCompensationToken;

    // This id will be passed to secondary root. 
    private readonly Variable<long> _compensationId;

    public CompensableActivity()
        : base()
    {
        _currentCompensationToken = new Variable<CompensationToken>();
        _currentCompensationId = new Variable<long>();
        _compensationId = new Variable<long>();
    }

    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }

    [DefaultValue(null)]
    [DependsOn("Variables")]
    public Activity Body { get; set; }

    [DefaultValue(null)]
    [DependsOn("Body")]
    public Activity CancellationHandler { get; set; }

    [DefaultValue(null)]
    [DependsOn("CancellationHandler")]
    public Activity CompensationHandler { get; set; }

    [DefaultValue(null)]
    [DependsOn("CompensationHandler")]
    public Activity ConfirmationHandler { get; set; }

    protected override bool CanInduceIdle => true;

    // Internal properties. 
    private CompensationParticipant CompensationParticipant
    {
        get
        {
            if (_compensationParticipant == null)
            {
                _compensationParticipant = new CompensationParticipant(_compensationId);

                if (CompensationHandler != null)
                {
                    _compensationParticipant.CompensationHandler = CompensationHandler;
                }

                if (ConfirmationHandler != null)
                {
                    _compensationParticipant.ConfirmationHandler = ConfirmationHandler;
                }

                if (CancellationHandler != null)
                {
                    _compensationParticipant.CancellationHandler = CancellationHandler;
                }
            }

            return _compensationParticipant;
        }
        set => _compensationParticipant = value;
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetVariablesCollection(Variables);

        metadata.SetImplementationVariablesCollection(
            new Collection<Variable>
            {
                _currentCompensationId,
                _currentCompensationToken,

                // Add the variables which are only used by the secondary root
                _compensationId
            });

        if (Body != null)
        {
            metadata.SetChildrenCollection(new Collection<Activity> { Body });
        }

        // Declare the handlers as public children.
        if (CompensationHandler != null)
        {
            metadata.AddImportedChild(CompensationHandler);
        }

        if (ConfirmationHandler != null)
        {
            metadata.AddImportedChild(ConfirmationHandler);
        }

        if (CancellationHandler != null)
        {
            metadata.AddImportedChild(CancellationHandler);
        }

        Collection<Activity> implementationChildren = new();

        if (!IsSingletonActivityDeclared(CompensationActivityStrings.WorkflowImplicitCompensationBehavior))
        {
            WorkflowCompensationBehavior workflowCompensationBehavior = new();
            DeclareSingletonActivity(CompensationActivityStrings.WorkflowImplicitCompensationBehavior, workflowCompensationBehavior);
            implementationChildren.Add(workflowCompensationBehavior);

            metadata.AddDefaultExtensionProvider(CreateCompensationExtension);
        }

        // Clear the cached handler values as workflow definition could be updated.
        CompensationParticipant = null;
        implementationChildren.Add(CompensationParticipant);

        metadata.SetImplementationChildrenCollection(implementationChildren);
    }

    private CompensationExtension CreateCompensationExtension() => new();

    internal override IList<Constraint> InternalGetConstraints() => new List<Constraint>(1) { noCompensableActivityInSecondaryRoot };

    private static Constraint NoCompensableActivityInSecondaryRoot()
    {
        DelegateInArgument<ValidationContext> validationContext = new() { Name = "validationContext" };
        DelegateInArgument<CompensableActivity> element = new() { Name = "element" };
        Variable<bool> assertFlag = new() { Name = "assertFlag", Default = true };
        Variable<IEnumerable<Activity>> elements = new() { Name = "elements" };
        Variable<int> index = new() { Name = "index" };

        return new Constraint<CompensableActivity>
        {
            Body = new ActivityAction<CompensableActivity, ValidationContext>
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
                        new Assign<IEnumerable<Activity>>
                        {
                            To = elements,
                            Value = new GetParentChain
                            {
                                ValidationContext = validationContext,
                            },
                        },
                        // Need to replace the lambda expression with a CodeActivity for partial trust.
                        // new While(env => (assertFlag.Get(env) != false) && index.Get(env) < elements.Get(env).Count())
                        new While
                        {
                            Condition = new WhileExpression
                            {
                                DisplayName = "env => (assertFlag.Get(env) != false) && index.Get(env) < elements.Get(env).Count())",
                                AssertFlag = new InArgument<bool>(assertFlag),
                                Index = new InArgument<int>(index),
                                Elements = new InArgument<IEnumerable<Activity>>(elements)
                            },

                            Body = new Sequence
                            {
                                Activities = 
                                {
                                    // Need to replace the lambda expression with a CodeActivity for partial trust.
                                    // new If(env => (elements.Get(env).ElementAt(index.Get(env))).GetType() == typeof(CompensationParticipant))
                                    new If
                                    {
                                        Condition = new IfExpression
                                        {
                                            DisplayName = "env => (elements.Get(env).ElementAt(index.Get(env))).GetType() == typeof(CompensationParticipant)",
                                            Elements = new InArgument<IEnumerable<Activity>>(elements),
                                            Index = new InArgument<int>(index)
                                        },

                                        Then = new Assign<bool>
                                        {
                                            To = assertFlag,
                                            Value = false                                                            
                                        },
                                    },
                                    new Assign<int>
                                    {
                                        To = index,
                                        // Need to replace the lambda expression for partial trust. Using Add expression activity instead of a CodeActivity here.
                                        // Value = new InArgument<int>(env => index.Get(env) + 1)
                                        Value = new InArgument<int>
                                        {
                                            Expression = new Add<int, int, int>
                                            {
                                                DisplayName = "(env => index.Get(env) + 1)",
                                                Left = new VariableValue<int>
                                                {
                                                    Variable = index
                                                },
                                                Right = 1,
                                            }
                                        }
                                    },
                                }
                            }
                        },                            
                        new AssertValidation
                        {
                            Assertion = new InArgument<bool>(assertFlag),
                            Message = new InArgument<string>(SR.NoCAInSecondaryRoot)   
                        }
                    }
                }
            }
        };
    }

    protected override void Execute(NativeActivityContext context)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        if (compensationExtension.IsWorkflowCompensationBehaviorScheduled)
        {
            ScheduleBody(context, compensationExtension);
        }
        else
        {
            compensationExtension.SetupWorkflowCompensationBehavior(context, new BookmarkCallback(OnWorkflowCompensationBehaviorScheduled), GetSingletonActivity(CompensationActivityStrings.WorkflowImplicitCompensationBehavior));
        }
    }

    protected override void Cancel(NativeActivityContext context) => context.CancelChildren();

    private void OnWorkflowCompensationBehaviorScheduled(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        ScheduleBody(context, compensationExtension);
    }

    private void ScheduleBody(NativeActivityContext context, CompensationExtension compensationExtension)
    {
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationToken parentToken = null;
        long parentCompensationId = CompensationToken.RootCompensationId;

        parentToken = (CompensationToken)context.Properties.Find(CompensationToken.PropertyName);

        if (parentToken != null)
        {
            if (compensationExtension.Get(parentToken.CompensationId).IsTokenValidInSecondaryRoot)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.NoCAInSecondaryRoot));
            }

            parentCompensationId = parentToken.CompensationId;
        }

        CompensationTokenData tokenData = new(compensationExtension.GetNextId(), parentCompensationId)
            {
                CompensationState = CompensationState.Active,
                DisplayName = DisplayName,
            };
        CompensationToken token = new(tokenData);

        context.Properties.Add(CompensationToken.PropertyName, token);

        _currentCompensationId.Set(context, token.CompensationId);
        _currentCompensationToken.Set(context, token);

        compensationExtension.Add(token.CompensationId, tokenData);

        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(tokenData.DisplayName, tokenData.CompensationState.ToString());
        }

        if (Body != null)
        {
            context.ScheduleActivity(Body, new CompletionCallback(OnBodyExecutionComplete));
        }
        else
        {
            //empty body case. Assume the body has completed successfully
            tokenData.CompensationState = CompensationState.Completed;
            if (TD.CompensationStateIsEnabled())
            {
                TD.CompensationState(tokenData.DisplayName, tokenData.CompensationState.ToString());
            }

            ScheduleSecondaryRoot(context, compensationExtension, tokenData);
        }
    }

    private void OnBodyExecutionComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData token = compensationExtension.Get(_currentCompensationId.Get(context));
        Fx.Assert(token != null, "CompensationTokenData must be valid");

        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            token.CompensationState = CompensationState.Completed;
            if (TD.CompensationStateIsEnabled())
            {
                TD.CompensationState(token.DisplayName, token.CompensationState.ToString());
            }

            if (context.IsCancellationRequested)
            {
                token.CompensationState = CompensationState.Compensating;
            }
        }
        else if (completedInstance.State == ActivityInstanceState.Canceled || completedInstance.State == ActivityInstanceState.Faulted)
        {
            // we check for faulted as well for one odd case where an exception can be thrown from the body activity itself. 
            token.CompensationState = CompensationState.Canceling;
        }
        else
        {
            Fx.Assert(false, "completedInstance in unexpected state");
        }

        ScheduleSecondaryRoot(context, compensationExtension, token);
    }

    private void ScheduleSecondaryRoot(NativeActivityContext context, CompensationExtension compensationExtension, CompensationTokenData token)
    {
        if (token.ParentCompensationId != CompensationToken.RootCompensationId)
        {
            CompensationTokenData parentToken = compensationExtension.Get(token.ParentCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Add(token);
        }
        else
        {
            CompensationTokenData parentToken = compensationExtension.Get(CompensationToken.RootCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Add(token);
        }

        // If we are going to Cancel, don't set the out arg...       
        if (Result != null && token.CompensationState == CompensationState.Completed)
        {
            Result.Set(context, _currentCompensationToken.Get(context));
        }

        Fx.Assert(token.BookmarkTable[CompensationBookmarkName.OnSecondaryRootScheduled] == null, "Bookmark should not be already initialized in the bookmark table.");
        token.BookmarkTable[CompensationBookmarkName.OnSecondaryRootScheduled] = context.CreateBookmark(new BookmarkCallback(OnSecondaryRootScheduled));

        _compensationId.Set(context, token.CompensationId);

        context.ScheduleSecondaryRoot(CompensationParticipant, context.Environment);
    }

    private void OnSecondaryRootScheduled(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = (long)value;

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        if (compensationToken.CompensationState == CompensationState.Canceling)
        {
            Fx.Assert(compensationToken.BookmarkTable[CompensationBookmarkName.Canceled] == null, "Bookmark should not be already initialized in the bookmark table.");
            compensationToken.BookmarkTable[CompensationBookmarkName.Canceled] = context.CreateBookmark(new BookmarkCallback(OnCanceledOrCompensated));

            compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.OnCancellation);
        }
        else if (compensationToken.CompensationState == CompensationState.Compensating)
        {
            Fx.Assert(compensationToken.BookmarkTable[CompensationBookmarkName.Compensated] == null, "Bookmark should not be already initialized in the bookmark table.");
            compensationToken.BookmarkTable[CompensationBookmarkName.Compensated] = context.CreateBookmark(new BookmarkCallback(OnCanceledOrCompensated));

            compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.OnCompensation);
        }
    }

    private void OnCanceledOrCompensated(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = (long)value;

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        switch (compensationToken.CompensationState)
        {
            case CompensationState.Canceling:
                compensationToken.CompensationState = CompensationState.Canceled;
                break;
            case CompensationState.Compensating:
                compensationToken.CompensationState = CompensationState.Compensated;
                break;
            default:
                break;
        }

        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(compensationToken.DisplayName, compensationToken.CompensationState.ToString());
        }

        AppCompletionCleanup(context, compensationExtension, compensationToken);

        // Mark the activity as canceled. 
        context.MarkCanceled();
    }

    private static void AppCompletionCleanup(NativeActivityContext context, CompensationExtension compensationExtension, CompensationTokenData compensationToken)
    {
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        // Remove the token from the parent! 
        if (compensationToken.ParentCompensationId != CompensationToken.RootCompensationId)
        {
            CompensationTokenData parentToken = compensationExtension.Get(compensationToken.ParentCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Remove(compensationToken);
        }
        else
        {
            // remove from workflow root...
            CompensationTokenData parentToken = compensationExtension.Get(CompensationToken.RootCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Remove(compensationToken);
        }

        compensationToken.RemoveBookmark(context, CompensationBookmarkName.Canceled);
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.Compensated);

        // Remove the token from the extension...
        compensationExtension.Remove(compensationToken.CompensationId);
    }
}

// In order to run in partial trust, we can't have lambda expressions that reference local variables. So this
// code activity replaces the lambda expression in this statement:
// While(env => (assertFlag.Get(env) != false) && index.Get(env) < elements.Get(env).Count())
internal class WhileExpression : CodeActivity<bool>
{
    public InArgument<bool> AssertFlag { get; set; }

    public InArgument<int> Index { get; set; }

    public InArgument<IEnumerable<Activity>> Elements { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument assertFlagArgument = new("AssertFlag", typeof(bool), ArgumentDirection.In);
        AssertFlag ??= new InArgument<bool>();
        metadata.Bind(AssertFlag, assertFlagArgument);

        RuntimeArgument indexArgument = new("Index", typeof(int), ArgumentDirection.In);
        Index ??= new InArgument<int>();
        metadata.Bind(Index, indexArgument);

        RuntimeArgument elementsArgument = new("Elements", typeof(IEnumerable<Activity>), ArgumentDirection.In);
        Elements ??= new InArgument<IEnumerable<Activity>>();
        metadata.Bind(Elements, elementsArgument);

        RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
        Result ??= new OutArgument<bool>();
        metadata.Bind(Result, resultArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                assertFlagArgument,
                indexArgument,
                elementsArgument,
                resultArgument
            });
    }

    protected override bool Execute(CodeActivityContext context)
    {
        // While(env => (assertFlag.Get(env) != false) && index.Get(env) < elements.Get(env).Count())
        return ((AssertFlag.Get(context) != false) && (Index.Get(context) < Elements.Get(context).Count()));
    }
}


// If(env => (elements.Get(env).ElementAt(index.Get(env))).GetType() == typeof(CompensationParticipant))
internal class IfExpression : CodeActivity<bool>
{
    public InArgument<IEnumerable<Activity>> Elements { get; set; }

    public InArgument<int> Index { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument elementsArgument = new("Elements", typeof(IEnumerable<Activity>), ArgumentDirection.In);
        Elements ??= new InArgument<IEnumerable<Activity>>();
        metadata.Bind(Elements, elementsArgument);

        RuntimeArgument indexArgument = new("Index", typeof(int), ArgumentDirection.In);
        Index ??= new InArgument<int>();
        metadata.Bind(Index, indexArgument);

        RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
        Result ??= new OutArgument<bool>();
        metadata.Bind(Result, resultArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                indexArgument,
                elementsArgument,
                resultArgument
            });
    }

    protected override bool Execute(CodeActivityContext context)
    {
        // If(env => (elements.Get(env).ElementAt(index.Get(env))).GetType() == typeof(CompensationParticipant))
        return (Elements.Get(context).ElementAt(Index.Get(context)).GetType() == typeof(CompensationParticipant));
    }
}
