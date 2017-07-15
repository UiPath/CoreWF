// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Linq.Expressions;
using System.Reflection;
using Test.Common.TestObjects.Activities.Collections;

namespace Test.Common.TestObjects.Activities
{
    public class TestInvokeMethod : TestActivity
    {
        private MemberCollection<TestArgument> _arguments;
        private MemberCollection<Type> _genericTypeArguments;

        public TestInvokeMethod(String methodName)
        {
            this.ProductActivity = new InvokeMethod();
            this.MethodName = methodName;

            _arguments = new MemberCollection<TestArgument>(AddArgument);
            _genericTypeArguments = new MemberCollection<Type>(AddGenericTypeArguments);
        }

        public TestInvokeMethod()
        {
            this.ProductActivity = new InvokeMethod();
        }

        public TestInvokeMethod(MethodInfo methodInfo)
            : this()
        {
            this.MethodName = methodInfo.Name;
            if (methodInfo.IsGenericMethod)
            {
                this.TargetType = methodInfo.DeclaringType;
            }

            _arguments = new MemberCollection<TestArgument>(AddArgument);
            _genericTypeArguments = new MemberCollection<Type>(AddGenericTypeArguments);
        }

        public TestInvokeMethod(string displayName, MethodInfo syncMethod)
            : this(syncMethod)
        {
            this.DisplayName = displayName;
        }

        public string MethodName
        {
            get
            {
                return this.ProductInvokeMethod.MethodName;
            }

            set
            {
                this.ProductInvokeMethod.MethodName = value;
            }
        }

        public Variable TargetObjectVariable
        {
            set
            {
                this.ProductInvokeMethod.TargetObject = Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(value.Type), value) as InArgument;
                this.TargetType = null;
            }
        }

        public TestArgument TargetObject
        {
            set
            {
                this.ProductInvokeMethod.TargetObject = (InArgument)value.ProductArgument;
                this.TargetType = null;
            }
        }

        public Type TargetType
        {
            get
            {
                return this.ProductInvokeMethod.TargetType;
            }
            set
            {
                this.ProductInvokeMethod.TargetType = value;
            }
        }

        public bool RunAsynchronously
        {
            get
            {
                return this.ProductInvokeMethod.RunAsynchronously;
            }

            set
            {
                this.ProductInvokeMethod.RunAsynchronously = value;
            }
        }

        public MemberCollection<TestArgument> Arguments
        {
            get
            {
                return _arguments;
            }
        }

        public MemberCollection<Type> GenericTypeArguments
        {
            get
            {
                return _genericTypeArguments;
            }
        }

        private InvokeMethod ProductInvokeMethod
        {
            get
            {
                return (InvokeMethod)this.ProductActivity;
            }
        }

        public void SetResultActivity<T>(TestActivity activity)
        {
            this.ProductInvokeMethod.Result = new OutArgument<T>((Activity<Location<T>>)activity.ProductActivity);
        }

        public void SetTargetObjectActivity<T>(TestActivity activity)
        {
            this.ProductInvokeMethod.TargetObject = new InArgument<T>((Activity<T>)activity.ProductActivity);
        }

        public void SetResultVariable<T>(Variable<T> variable)
        {
            this.ProductInvokeMethod.Result = new OutArgument<T>(variable);
        }

        public void SetResultExpression<T>(Expression<Func<ActivityContext, T>> expression)
        {
            this.ProductInvokeMethod.Result = new OutArgument<T>(expression);
        }

        private void AddArgument(TestArgument item)
        {
            this.ProductInvokeMethod.Parameters.Add(item.ProductArgument);
        }

        private void AddGenericTypeArguments(Type type)
        {
            this.ProductInvokeMethod.GenericTypeArguments.Add(type);
        }
    }
}
