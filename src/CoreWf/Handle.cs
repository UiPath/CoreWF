// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Runtime.Serialization;

namespace CoreWf
{
    [DataContract]
    public abstract class Handle
    {
        private ActivityInstance _owner;

        // We check uninitialized because it should be false more often
        private bool _isUninitialized;

        protected Handle()
        {
            _isUninitialized = true;
        }

        public ActivityInstance Owner
        {
            get
            {
                return _owner;
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
            get { return _owner; }
            set { _owner = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "isUninitialized")]
        internal bool SerializedIsUninitialized
        {
            get { return _isUninitialized; }
            set { _isUninitialized = value; }
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
                return !_isUninitialized;
            }
        }

        internal static string GetPropertyName(Type handleType)
        {
            Fx.Assert(TypeHelper.AreTypesCompatible(handleType, typeof(Handle)), "must pass in a Handle-based type here");
            return handleType.FullName;
        }

        internal void Initialize(HandleInitializationContext context)
        {
            _owner = context.OwningActivityInstance;
            _isUninitialized = false;

            OnInitialize(context);
        }

        internal void Reinitialize(ActivityInstance owner)
        {
            _owner = owner;
        }

        internal void Uninitialize(HandleInitializationContext context)
        {
            OnUninitialize(context);
            _isUninitialized = true;
        }

        protected virtual void OnInitialize(HandleInitializationContext context)
        {
        }

        protected virtual void OnUninitialize(HandleInitializationContext context)
        {
        }

        protected void ThrowIfUninitialized()
        {
            if (_isUninitialized)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.HandleNotInitialized));
            }
        }
    }
}


