// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CoreWf
{
    public abstract class DelegateArgument : LocationReference
    {
        private ArgumentDirection _direction;
        private RuntimeDelegateArgument _runtimeArgument;
        private string _name;
        private int _cacheId;

        internal DelegateArgument()
        {
            this.Id = -1;
        }

        [DefaultValue(null)]
        public new string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        protected override string NameCore
        {
            get
            {
                return _name;
            }
        }

        public ArgumentDirection Direction
        {
            get
            {
                return _direction;
            }
            internal set
            {
                _direction = value;
            }
        }

        internal Activity Owner
        {
            get;
            private set;
        }

        internal bool IsInTree
        {
            get
            {
                return this.Owner != null;
            }
        }

        internal void ThrowIfNotInTree()
        {
            if (!this.IsInTree)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentMustBeReferenced(this.Name)));
            }
        }

        internal void Bind(RuntimeDelegateArgument runtimeArgument)
        {
            _runtimeArgument = runtimeArgument;
        }

        internal bool InitializeRelationship(Activity parent, ref IList<ValidationError> validationErrors)
        {
            if (_cacheId == parent.CacheId)
            {
                Fx.Assert(this.Owner != null, "must have an owner here");
                ValidationError validationError = new ValidationError(SR.DelegateArgumentAlreadyInUseOnActivity(this.Name, parent.DisplayName, this.Owner.DisplayName), this.Owner);
                ActivityUtilities.Add(ref validationErrors, validationError);

                // Get out early since we've already initialized this argument.
                return false;
            }

            this.Owner = parent;
            _cacheId = parent.CacheId;

            return true;
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public object Get(ActivityContext context)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            return context.GetValue<object>((LocationReference)this);
        }

        public override Location GetLocation(ActivityContext context)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            ThrowIfNotInTree();

            if (!context.AllowChainedEnvironmentAccess)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
            }

            Location location;
            if (!context.Environment.TryGetLocation(this.Id, this.Owner, out location))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
            }

            return location;
        }

        // Only used by the debugger
        internal Location InternalGetLocation(LocationEnvironment environment)
        {
            Fx.Assert(this.IsInTree, "DelegateArgument must be opened");

            Location location;
            if (!environment.TryGetLocation(this.Id, this.Owner, out location))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
            }
            return location;
        }

        internal abstract Location CreateLocation();
    }
}
