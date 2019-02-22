// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public abstract class Handle
    {
        private ActivityInstance owner;

        // We check uninitialized because it should be false more often
        private bool isUninitialized;

        protected Handle()
        {
            this.isUninitialized = true;
        }

        public ActivityInstance Owner
        {
            get
            {
                return this.owner;
            }
        }

        public string ExecutionPropertyName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "owner")]
        internal ActivityInstance SerializedOwner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "isUninitialized")]
        internal bool SerializedIsUninitialized
        {
            get { return this.isUninitialized; }
            set { this.isUninitialized = value; }
        }

        [DataMember(EmitDefaultValue = false)]
        internal bool CanBeRemovedWithExecutingChildren
        {
            get;
            set;
        }

        internal bool IsInitialized
        {
            get
            {
                return !this.isUninitialized;
            }
        }

        internal static string GetPropertyName(Type handleType)
        {
            Fx.Assert(TypeHelper.AreTypesCompatible(handleType, typeof(Handle)), "must pass in a Handle-based type here");
            return handleType.FullName;
        }

        internal void Initialize(HandleInitializationContext context)
        {
            this.owner = context.OwningActivityInstance;
            this.isUninitialized = false;

            OnInitialize(context);
        }

        internal void Reinitialize(ActivityInstance owner)
        {
            this.owner = owner;
        }

        internal void Uninitialize(HandleInitializationContext context)
        {
            OnUninitialize(context);
            this.isUninitialized = true;
        }

        protected virtual void OnInitialize(HandleInitializationContext context)
        {
        }

        protected virtual void OnUninitialize(HandleInitializationContext context)
        {
        }

        protected void ThrowIfUninitialized()
        {
            if (this.isUninitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.HandleNotInitialized));
            }
        }
    }
}


