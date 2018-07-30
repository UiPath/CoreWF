// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using CoreWf.Runtime.Collections;
    using Portable.Xaml.Markup;
    using CoreWf.Expressions;
    using System.Reflection;
    using System.Threading;
    using System;
    using CoreWf.Runtime;
    using CoreWf.Internals;

    [ContentProperty("Parameters")]
    public sealed class InvokeMethod : AsyncCodeActivity
    {
        private Collection<Argument> parameters;
        private Collection<Type> genericTypeArguments;
        private MethodResolver methodResolver;
        private MethodExecutor methodExecutor;
        private RuntimeArgument resultArgument;
        private static readonly MruCache<MethodInfo, Func<object, object[], object>> funcCache =
            new MruCache<MethodInfo, Func<object, object[], object>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();


        public Collection<Type> GenericTypeArguments
        {
            get
            {
                if (this.genericTypeArguments == null)
                {
                    this.genericTypeArguments = new ValidatingCollection<Type>
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
                }
                return this.genericTypeArguments;
            }
        }

        public string MethodName
        {
            get;
            set;
        }

        public Collection<Argument> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.parameters = new ValidatingCollection<Argument>
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
                }
                return this.parameters;
            }
        }

        [DefaultValue(null)]
        public OutArgument Result
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public InArgument TargetObject
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public Type TargetType
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool RunAsynchronously
        {
            get;
            set;
        }


        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>();

            Type targetObjectType = TypeHelper.ObjectType;

            if (this.TargetObject != null)
            {
                targetObjectType = this.TargetObject.ArgumentType;
            }

            RuntimeArgument targetObjectArgument = new RuntimeArgument("TargetObject", targetObjectType, ArgumentDirection.In);
            metadata.Bind(this.TargetObject, targetObjectArgument);
            arguments.Add(targetObjectArgument);

            Type resultType = TypeHelper.ObjectType;

            if (this.Result != null)
            {
                resultType = this.Result.ArgumentType;
            }

            this.resultArgument = new RuntimeArgument("Result", resultType, ArgumentDirection.Out);
            metadata.Bind(this.Result, this.resultArgument);
            arguments.Add(resultArgument);

            // Parameters are named according to MethodInfo name if DetermineMethodInfo 
            // succeeds, otherwise arbitrary names are used.
            this.methodResolver = CreateMethodResolver();
            this.methodResolver.DetermineMethodInfo(metadata, funcCache, locker, ref this.methodExecutor);
            this.methodResolver.RegisterParameters(arguments);

            metadata.SetArgumentsCollection(arguments);

            this.methodResolver.Trace();

            if (this.methodExecutor != null)
            {
                this.methodExecutor.Trace(this);
            }
        }


        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            return this.methodExecutor.BeginExecuteMethod(context, callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            this.methodExecutor.EndExecuteMethod(context, result);
        }

        private MethodResolver CreateMethodResolver()
        {
            MethodResolver resolver = new MethodResolver
                {
                    MethodName = this.MethodName,
                    RunAsynchronously = this.RunAsynchronously,
                    TargetType = this.TargetType,
                    TargetObject = this.TargetObject,
                    GenericTypeArguments = this.GenericTypeArguments,
                    Parameters = this.Parameters,
                    Result = this.resultArgument,
                    Parent = this
                };

            if (this.Result != null)
            {
                resolver.ResultType = this.Result.ArgumentType;
            }
            else
            {
                resolver.ResultType = typeof(object);
            }

            return resolver;
        }

    }
}
