// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Hosting
{
    using System;
    using System.Activities.Runtime;

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
        private readonly Func<T> providerFunction;
        private bool hasGeneratedValue;

        public WorkflowInstanceExtensionProvider(Func<T> providerFunction)
            : base()
        {
            this.providerFunction = providerFunction;
            base.Type = typeof(T);
        }

        public override object ProvideValue()
        {
            T value = this.providerFunction();
            if (!this.hasGeneratedValue)
            {
                base.GeneratedTypeMatchesDeclaredType = object.ReferenceEquals(value.GetType(), this.Type);
                this.hasGeneratedValue = true;
            }

            return value;
        }
    }
}
