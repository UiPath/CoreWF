// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Transactions;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class TransactionScope : NativeActivity
{
    private const IsolationLevel defaultIsolationLevel = default;
    private InArgument<TimeSpan> _timeout;
    private bool _isTimeoutSetExplicitly;
    private readonly Variable<RuntimeTransactionHandle> _runtimeTransactionHandle;
    private bool _abortInstanceOnTransactionFailure;
    private bool _abortInstanceFlagWasExplicitlySet;
    private Delay _nestedScopeTimeoutWorkflow;
    private readonly Variable<bool> _delayWasScheduled;
    private readonly Variable<TimeSpan> _nestedScopeTimeout;
    private readonly Variable<ActivityInstance> _nestedScopeTimeoutActivityInstance;
    private static readonly string runtimeTransactionHandlePropertyName = typeof(RuntimeTransactionHandle).FullName;
    private const string AbortInstanceOnTransactionFailurePropertyName = "AbortInstanceOnTransactionFailure";
    private const string IsolationLevelPropertyName = "IsolationLevel";
    private const string BodyPropertyName = "Body";

    public TransactionScope()
        : base()
    {
        _timeout = new InArgument<TimeSpan>(TimeSpan.FromMinutes(1));
        _runtimeTransactionHandle = new Variable<RuntimeTransactionHandle>();
        _abortInstanceOnTransactionFailure = true;
        _nestedScopeTimeout = new Variable<TimeSpan>();
        _delayWasScheduled = new Variable<bool>();
        _nestedScopeTimeoutActivityInstance = new Variable<ActivityInstance>();

        Constraints.Add(TransactionScope.ProcessParentChainConstraints());
        Constraints.Add(TransactionScope.ProcessChildSubtreeConstraints());
    }

    [DefaultValue(null)]
    public Activity Body { get; set; }

    [DefaultValue(true)]
    public bool AbortInstanceOnTransactionFailure
    {
        get => _abortInstanceOnTransactionFailure;
        set
        {
            _abortInstanceOnTransactionFailure = value;
            _abortInstanceFlagWasExplicitlySet = true;
        }
    }

    public IsolationLevel IsolationLevel { get; set; }

    public InArgument<TimeSpan> Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            _isTimeoutSetExplicitly = true;
        }
    }

    private Delay NestedScopeTimeoutWorkflow
    {
        get
        {
            _nestedScopeTimeoutWorkflow ??= new Delay
            {
                Duration = new InArgument<TimeSpan>(_nestedScopeTimeout)
            };
            return _nestedScopeTimeoutWorkflow;
        }
    }

    protected override bool CanInduceIdle => true;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeIsolationLevel() => IsolationLevel != defaultIsolationLevel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeTimeout() => _isTimeoutSetExplicitly;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument timeoutArgument = new("Timeout", typeof(TimeSpan), ArgumentDirection.In, false);
        metadata.Bind(Timeout, timeoutArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { timeoutArgument });
        metadata.AddImplementationChild(NestedScopeTimeoutWorkflow);

        if (Body != null)
        {
            metadata.AddChild(Body);
        }

        metadata.AddImplementationVariable(_runtimeTransactionHandle);
        metadata.AddImplementationVariable(_nestedScopeTimeout);
        metadata.AddImplementationVariable(_delayWasScheduled);
        metadata.AddImplementationVariable(_nestedScopeTimeoutActivityInstance);
    }

    private static Constraint ProcessParentChainConstraints()
    {
        DelegateInArgument<TransactionScope> element = new() { Name = "element" };
        DelegateInArgument<ValidationContext> validationContext = new() { Name = "validationContext" };
        DelegateInArgument<Activity> parent = new() { Name = "parent" };

        return new Constraint<TransactionScope>
        {
            Body = new ActivityAction<TransactionScope, ValidationContext>
            {
                Argument1 = element,
                Argument2 = validationContext,
                Handler = new Sequence
                {
                    Activities = 
                    {
                        new ForEach<Activity>
                        {
                            Values = new GetParentChain
                            {
                                ValidationContext = validationContext,
                            },
                            Body = new ActivityAction<Activity>
                            {
                                Argument = parent,
                                Handler = new Sequence                                   
                                {
                                    Activities = 
                                    {
                                        new If()
                                        {
                                            Condition = new Equal<Type, Type, bool>
                                            {
                                                Left = new ObtainType
                                                {
                                                    Input = parent,
                                                },
                                                Right = new InArgument<Type>(context => typeof(TransactionScope))
                                            },
                                            Then = new Sequence
                                            {
                                                Activities = 
                                                {
                                                    new AssertValidation
                                                    {
                                                        IsWarning = true,
                                                        Assertion = new AbortInstanceFlagValidator
                                                        {
                                                            ParentActivity = parent,
                                                            TransactionScope = new InArgument<TransactionScope>(element)
                                                        },
                                                        Message = new InArgument<string>(SR.AbortInstanceOnTransactionFailureDoesNotMatch),
                                                        PropertyName = AbortInstanceOnTransactionFailurePropertyName
                                                    },
                                                    new AssertValidation
                                                    {
                                                        Assertion = new IsolationLevelValidator
                                                        {
                                                            ParentActivity = parent,
                                                            //CurrentIsolationLevel = new InArgument<IsolationLevel>(context => element.Get(context).IsolationLevel)
                                                            CurrentIsolationLevel = new InArgument<IsolationLevel>
                                                            {
                                                                Expression = new IsolationLevelValue
                                                                {
                                                                    Scope = element
                                                                }
                                                            }
                                                        },
                                                        Message = new InArgument<string>(SR.IsolationLevelValidation),
                                                        PropertyName = IsolationLevelPropertyName
                                                    }                                                      

                                                }
                                            }
                                                
                                        }
                                    }
                                }                                   
                            }
                        }
                    }
                }
            }
        };
    }

    private static Constraint ProcessChildSubtreeConstraints()
    {
        DelegateInArgument<TransactionScope> element = new() { Name = "element" };
        DelegateInArgument<ValidationContext> validationContext = new() { Name = "validationContext" };
        DelegateInArgument<Activity> child = new() { Name = "child" };
        Variable<bool> nestedCompensableActivity = new();

        return new Constraint<TransactionScope>
        {
            Body = new ActivityAction<TransactionScope, ValidationContext>
            {
                Argument1 = element,
                Argument2 = validationContext,
                Handler = new Sequence
                {
                    Variables = { nestedCompensableActivity },
                    Activities = 
                    {                            
                        new ForEach<Activity>
                        {
                            Values = new GetChildSubtree
                            {
                                ValidationContext = validationContext,
                            },
                            Body = new ActivityAction<Activity>
                            {
                                Argument = child,
                                Handler = new Sequence                                   
                                {
                                    Activities = 
                                    {
                                        new If()
                                        {
                                            Condition = new Equal<Type, Type, bool>()
                                            {
                                                    Left = new ObtainType
                                                    {
                                                        Input = new InArgument<Activity>(child)
                                                    },
                                                    Right = new InArgument<Type>(context => typeof(CompensableActivity))
                                            },
                                            Then = new Assign<bool>
                                            {
                                                To = new OutArgument<bool>(nestedCompensableActivity),
                                                Value = new InArgument<bool>(true)
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new AssertValidation()
                        {
                            Assertion = new InArgument<bool>(new Not<bool, bool> { Operand = new VariableValue<bool> { Variable = nestedCompensableActivity } }),
                            Message = new InArgument<string>(SR.CompensableActivityInsideTransactionScopeActivity),
                            PropertyName = BodyPropertyName
                        }
                    }
                }
            }
        };
    }

    protected override void Execute(NativeActivityContext context)
    {
        RuntimeTransactionHandle transactionHandle = _runtimeTransactionHandle.Get(context);
        Fx.Assert(transactionHandle != null, "RuntimeTransactionHandle is null");

        if (context.Properties.Find(runtimeTransactionHandlePropertyName) is not RuntimeTransactionHandle foundHandle)
        {
            //Note, once the property is registered, we cannot change the state of this flag
            transactionHandle.AbortInstanceOnTransactionFailure = AbortInstanceOnTransactionFailure;
            context.Properties.Add(transactionHandle.ExecutionPropertyName, transactionHandle);
        }
        else
        {
            //nested case
            //foundHandle.IsRuntimeOwnedTransaction will be true only in the Invoke case within an ambient Sys.Tx transaction. 
            //If this TSA is nested inside the ambient transaction from Invoke, then the AbortInstanceFlag is always false since the RTH corresponding to the ambient
            //transaction has this flag as false. In this case, we ignore if this TSA has this flag explicitly set to true. 
            if (!foundHandle.IsRuntimeOwnedTransaction && _abortInstanceFlagWasExplicitlySet && (foundHandle.AbortInstanceOnTransactionFailure != AbortInstanceOnTransactionFailure))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AbortInstanceOnTransactionFailureDoesNotMatch));
            }

            if (foundHandle.SuppressTransaction)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotNestTransactionScopeWhenAmbientHandleIsSuppressed(DisplayName)));
            }
            transactionHandle = foundHandle;
        }

        Transaction transaction = transactionHandle.GetCurrentTransaction(context);
        //Check if there is already a transaction (Requires Semantics)
        if (transaction == null)
        {
            //If not, request one..
            transactionHandle.RequestTransactionContext(context, OnContextAcquired, null);
        }
        else
        {
            //Most likely, you are inside a nested TSA
            if (transaction.IsolationLevel != IsolationLevel)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.IsolationLevelValidation));
            }

            //Check if the nested TSA had a timeout specified explicitly
            if (_isTimeoutSetExplicitly)
            {
                TimeSpan timeout = Timeout.Get(context);
                _delayWasScheduled.Set(context, true);
                _nestedScopeTimeout.Set(context, timeout);

                _nestedScopeTimeoutActivityInstance.Set(context, context.ScheduleActivity(NestedScopeTimeoutWorkflow, new CompletionCallback(OnDelayCompletion)));
            }

            //execute the Body under the current runtime transaction
            ScheduleBody(context);
        }
    }

    private void OnContextAcquired(NativeActivityTransactionContext context, object state)
    {
        Fx.Assert(context != null, "ActivityTransactionContext was null");

        TimeSpan transactionTimeout = Timeout.Get(context);
        TransactionOptions transactionOptions = new()
        {
            IsolationLevel = IsolationLevel,
            Timeout = transactionTimeout
        };

        context.SetRuntimeTransaction(new CommittableTransaction(transactionOptions));

        ScheduleBody(context);
    }

    private void ScheduleBody(NativeActivityContext context)
    {
        if (Body != null)
        {
            context.ScheduleActivity(Body, new CompletionCallback(OnCompletion));
        }
    }

    private void OnCompletion(NativeActivityContext context, ActivityInstance instance)
    {
        RuntimeTransactionHandle transactionHandle = _runtimeTransactionHandle.Get(context);
        Fx.Assert(transactionHandle != null, "RuntimeTransactionHandle is null");

        if (_delayWasScheduled.Get(context))
        {
            transactionHandle.CompleteTransaction(context, new BookmarkCallback(OnTransactionComplete));
        }
        else
        {
            transactionHandle.CompleteTransaction(context);
        }
    }

    private void OnDelayCompletion(NativeActivityContext context, ActivityInstance instance)
    {
        if (instance.State == ActivityInstanceState.Closed)
        {
            RuntimeTransactionHandle handle = context.Properties.Find(runtimeTransactionHandlePropertyName) as RuntimeTransactionHandle;
            Fx.Assert(handle != null, "Internal error.. If we are here, there ought to be an ambient transaction handle");
            handle.GetCurrentTransaction(context).Rollback();
        }
    }

    private void OnTransactionComplete(NativeActivityContext context, Bookmark bookmark, object state)
    {
        Fx.Assert(_delayWasScheduled.Get(context), "Internal error..Delay should have been scheduled if we are here");
        ActivityInstance delayActivityInstance = _nestedScopeTimeoutActivityInstance.Get(context);
        if (delayActivityInstance != null)
        {
            context.CancelChild(delayActivityInstance);
        }
    }

    private class ObtainType : CodeActivity<Type>
    {
        public ObtainType() { }

        public InArgument<Activity> Input { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument inputArgument = new("Input", typeof(Activity), ArgumentDirection.In);
            Input ??= new InArgument<Activity>();
            metadata.Bind(Input, inputArgument);

            RuntimeArgument resultArgument = new("Result", typeof(Type), ArgumentDirection.Out);
            Result ??= new OutArgument<Type>();
            metadata.Bind(Result, resultArgument);

            metadata.SetArgumentsCollection(
                new Collection<RuntimeArgument>
                {
                    inputArgument,
                    resultArgument
                });
        }

        protected override Type Execute(CodeActivityContext context) => Input.Get(context).GetType();
    }

    private class IsolationLevelValue : CodeActivity<IsolationLevel>
    {
        public InArgument<TransactionScope> Scope { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument scopeArgument = new("Scope", typeof(TransactionScope), ArgumentDirection.In);
            Scope ??= new InArgument<TransactionScope>();
            metadata.Bind(Scope, scopeArgument);

            RuntimeArgument resultArgument = new("Result", typeof(IsolationLevel), ArgumentDirection.Out);
            Result ??= new OutArgument<IsolationLevel>();
            metadata.Bind(Result, resultArgument);

            metadata.SetArgumentsCollection(
                new Collection<RuntimeArgument>
                {
                    scopeArgument,
                    resultArgument
                });
        }

        protected override IsolationLevel Execute(CodeActivityContext context) => Scope.Get(context).IsolationLevel;
    }

    private class IsolationLevelValidator : CodeActivity<bool>
    {
        public InArgument<Activity> ParentActivity { get; set; }

        public InArgument<IsolationLevel> CurrentIsolationLevel { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument parentActivityArgument = new("ParentActivity", typeof(Activity), ArgumentDirection.In);
            ParentActivity ??= new InArgument<Activity>();
            metadata.Bind(ParentActivity, parentActivityArgument);

            RuntimeArgument isoLevelArgument = new("CurrentIsolationLevel", typeof(IsolationLevel), ArgumentDirection.In);
            CurrentIsolationLevel ??= new InArgument<IsolationLevel>();
            metadata.Bind(CurrentIsolationLevel, isoLevelArgument);

            RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
            Result ??= new OutArgument<bool>();
            metadata.Bind(Result, resultArgument);

            metadata.SetArgumentsCollection(
                new Collection<RuntimeArgument>
                {
                    parentActivityArgument,
                    isoLevelArgument,
                    resultArgument
                });
        }

        protected override bool Execute(CodeActivityContext context)
        {
            Activity parent = ParentActivity.Get(context);

            if (parent != null)
            {
                TransactionScope transactionScope = parent as TransactionScope;
                Fx.Assert(transactionScope != null, "ParentActivity was not of expected type");

                return transactionScope.IsolationLevel == CurrentIsolationLevel.Get(context);
            }
            else
            {
                return true;
            }
        }
    }

    private class AbortInstanceFlagValidator : CodeActivity<bool>
    {
        public InArgument<Activity> ParentActivity { get; set; }

        public InArgument<TransactionScope> TransactionScope { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument parentActivityArgument = new("ParentActivity", typeof(Activity), ArgumentDirection.In);
            ParentActivity ??= new InArgument<Activity>();
            metadata.Bind(ParentActivity, parentActivityArgument);

            RuntimeArgument txScopeArgument = new("TransactionScope", typeof(TransactionScope), ArgumentDirection.In);
            TransactionScope ??= new InArgument<TransactionScope>();
            metadata.Bind(TransactionScope, txScopeArgument);

            RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
            Result ??= new OutArgument<bool>();
            metadata.Bind(Result, resultArgument);

            metadata.SetArgumentsCollection(
                new Collection<RuntimeArgument>
                {
                    parentActivityArgument,
                    txScopeArgument,
                    resultArgument
                });
        }

        protected override bool Execute(CodeActivityContext context)
        {
            Activity parent = ParentActivity.Get(context);

            if (parent != null)
            {
                TransactionScope parentTransactionScope = parent as TransactionScope;
                Fx.Assert(parentTransactionScope != null, "ParentActivity was not of expected type");
                TransactionScope currentTransactionScope = TransactionScope.Get(context);

                if (parentTransactionScope.AbortInstanceOnTransactionFailure != currentTransactionScope.AbortInstanceOnTransactionFailure)
                {
                    //If the Inner TSA was default and still different from outer, we dont flag validation warning. See design spec for all variations
                    return !currentTransactionScope._abortInstanceFlagWasExplicitlySet;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
