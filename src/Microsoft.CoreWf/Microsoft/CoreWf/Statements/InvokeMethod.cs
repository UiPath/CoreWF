// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Expressions;
using CoreWf.Runtime;
using CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace CoreWf.Statements
{
    //[ContentProperty("Parameters")]
    public sealed class InvokeMethod : AsyncCodeActivity
    {
        private Collection<Argument> _parameters;
        private Collection<Type> _genericTypeArguments;

        private MethodResolver _methodResolver;
        private MethodExecutor _methodExecutor;
        private RuntimeArgument _resultArgument;

        private static MruCache<MethodInfo, Func<object, object[], object>> s_funcCache =
            new MruCache<MethodInfo, Func<object, object[], object>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static ReaderWriterLockSlim s_locker = new ReaderWriterLockSlim();


        public Collection<Type> GenericTypeArguments
        {
            get
            {
                if (_genericTypeArguments == null)
                {
                    _genericTypeArguments = new ValidatingCollection<Type>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _genericTypeArguments;
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
                if (_parameters == null)
                {
                    _parameters = new ValidatingCollection<Argument>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _parameters;
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

            _resultArgument = new RuntimeArgument("Result", resultType, ArgumentDirection.Out);
            metadata.Bind(this.Result, _resultArgument);
            arguments.Add(_resultArgument);

            // Parameters are named according to MethodInfo name if DetermineMethodInfo 
            // succeeds, otherwise arbitrary names are used.
            _methodResolver = CreateMethodResolver();
            _methodResolver.DetermineMethodInfo(metadata, s_funcCache, s_locker, ref _methodExecutor);
            _methodResolver.RegisterParameters(arguments);

            metadata.SetArgumentsCollection(arguments);

            _methodResolver.Trace();

            if (_methodExecutor != null)
            {
                _methodExecutor.Trace(this);
            }
        }


        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            return _methodExecutor.BeginExecuteMethod(context, callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            _methodExecutor.EndExecuteMethod(context, result);
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
                Result = _resultArgument,
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
