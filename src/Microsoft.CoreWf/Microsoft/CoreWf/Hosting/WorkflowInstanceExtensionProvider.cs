// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;

namespace Microsoft.CoreWf.Hosting
{
    internal abstract class WorkflowInstanceExtensionProvider
    {
        protected WorkflowInstanceExtensionProvider()
        {
        }

        public Type Type
        {
            get;
            protected set;
        }

        protected bool GeneratedTypeMatchesDeclaredType
        {
            get;
            set;
        }

        public abstract object ProvideValue();

        public bool IsMatch<TTarget>(object value)
            where TTarget : class
        {
            Fx.Assert(value != null, "extension providers never return a null extension");
            if (value is TTarget)
            {
                if (this.GeneratedTypeMatchesDeclaredType)
                {
                    return true;
                }
                else
                {
                    return TypeHelper.AreReferenceTypesCompatible(this.Type, typeof(TTarget));
                }
            }
            else
            {
                return false;
            }
        }
    }

    internal class WorkflowInstanceExtensionProvider<T> : WorkflowInstanceExtensionProvider
        where T : class
    {
        private Func<T> _providerFunction;
        private bool _hasGeneratedValue;

        public WorkflowInstanceExtensionProvider(Func<T> providerFunction)
            : base()
        {
            _providerFunction = providerFunction;
            base.Type = typeof(T);
        }

        public override object ProvideValue()
        {
            T value = _providerFunction();
            if (!_hasGeneratedValue)
            {
                base.GeneratedTypeMatchesDeclaredType = object.ReferenceEquals(value.GetType(), this.Type);
                _hasGeneratedValue = true;
            }

            return value;
        }
    }
}
