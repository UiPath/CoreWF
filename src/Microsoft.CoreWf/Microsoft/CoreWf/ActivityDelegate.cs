// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace Microsoft.CoreWf
{
    public abstract class ActivityDelegate
    {
        internal static string ArgumentName = "Argument";
        internal static string Argument1Name = "Argument1";
        internal static string Argument2Name = "Argument2";
        internal static string Argument3Name = "Argument3";
        internal static string Argument4Name = "Argument4";
        internal static string Argument5Name = "Argument5";
        internal static string Argument6Name = "Argument6";
        internal static string Argument7Name = "Argument7";
        internal static string Argument8Name = "Argument8";
        internal static string Argument9Name = "Argument9";
        internal static string Argument10Name = "Argument10";
        internal static string Argument11Name = "Argument11";
        internal static string Argument12Name = "Argument12";
        internal static string Argument13Name = "Argument13";
        internal static string Argument14Name = "Argument14";
        internal static string Argument15Name = "Argument15";
        internal static string Argument16Name = "Argument16";
        internal static string ResultArgumentName = "Result";

        private Activity _owner;
        private bool _isDisplayNameSet;
        private string _displayName;
        private IList<RuntimeDelegateArgument> _delegateParameters;
        private int _cacheId;
        private ActivityCollectionType _parentCollectionType;

        protected ActivityDelegate()
        {
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    _displayName = this.GetType().Name;
                }

                return _displayName;
            }
            set
            {
                _isDisplayNameSet = true;
                _displayName = value;
            }
        }

        [DefaultValue(null)]
        public Activity Handler
        {
            get;
            set;
        }

        internal LocationReferenceEnvironment Environment
        {
            get;
            set;
        }

        internal Activity Owner
        {
            get
            {
                return _owner;
            }
        }

        internal ActivityCollectionType ParentCollectionType
        {
            get
            {
                return _parentCollectionType;
            }
        }

        internal IList<RuntimeDelegateArgument> RuntimeDelegateArguments
        {
            get
            {
                if (_delegateParameters != null)
                {
                    return _delegateParameters;
                }

                return new ReadOnlyCollection<RuntimeDelegateArgument>(InternalGetRuntimeDelegateArguments());
            }
        }

        protected internal virtual DelegateOutArgument GetResultArgument()
        {
            return null;
        }

        protected virtual void OnGetRuntimeDelegateArguments(IList<RuntimeDelegateArgument> runtimeDelegateArguments)
        {
            foreach (var property in this.GetType().GetTypeInfo().DeclaredProperties)
            {
                ArgumentDirection direction;
                Type innerType;
                if (ActivityUtilities.TryGetDelegateArgumentDirectionAndType(property.PropertyType, out direction, out innerType))
                {
                    runtimeDelegateArguments.Add(new RuntimeDelegateArgument(property.Name, innerType, direction, (DelegateArgument)property.GetValue(this)));
                }
            }
        }

        internal virtual IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
        {
            IList<RuntimeDelegateArgument> result = new List<RuntimeDelegateArgument>();
            OnGetRuntimeDelegateArguments(result);
            return result;
        }

        internal void InternalCacheMetadata()
        {
            _delegateParameters = new ReadOnlyCollection<RuntimeDelegateArgument>(InternalGetRuntimeDelegateArguments());
        }

        internal bool CanBeScheduledBy(Activity parent)
        {
            // fast path if we're the sole (or first) child
            if (object.ReferenceEquals(parent, _owner))
            {
                return _parentCollectionType != ActivityCollectionType.Imports;
            }
            else
            {
                return parent.Delegates.Contains(this) || parent.ImplementationDelegates.Contains(this);
            }
        }

        internal bool InitializeRelationship(Activity parent, ActivityCollectionType collectionType, ref IList<ValidationError> validationErrors)
        {
            if (_cacheId == parent.CacheId)
            {
                Fx.Assert(_owner != null, "We must have set the owner when we set the cache ID");

                // This means that we've already encountered a parent in the tree

                // Validate that it is visible.

                // In order to see the activity the new parent must be
                // in the implementation IdSpace of an activity which has
                // a public reference to it.
                Activity referenceTarget = parent.MemberOf.Owner;

                if (referenceTarget == null)
                {
                    Activity handler = this.Handler;

                    if (handler == null)
                    {
                        ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityDelegateCannotBeReferencedWithoutTargetNoHandler(parent.DisplayName, _owner.DisplayName), false, parent));
                    }
                    else
                    {
                        ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityDelegateCannotBeReferencedWithoutTarget(handler.DisplayName, parent.DisplayName, _owner.DisplayName), false, parent));
                    }

                    return false;
                }
                else if (!referenceTarget.Delegates.Contains(this) && !referenceTarget.ImportedDelegates.Contains(this))
                {
                    Activity handler = this.Handler;

                    if (handler == null)
                    {
                        ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityDelegateCannotBeReferencedNoHandler(parent.DisplayName, referenceTarget.DisplayName, _owner.DisplayName), false, parent));
                    }
                    else
                    {
                        ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityDelegateCannotBeReferenced(handler.DisplayName, parent.DisplayName, referenceTarget.DisplayName, _owner.DisplayName), false, parent));
                    }

                    return false;
                }

                // This is a valid reference so we want to allow
                // normal processing to proceed.
                return true;
            }

            _owner = parent;
            _cacheId = parent.CacheId;
            _parentCollectionType = collectionType;
            InternalCacheMetadata();

            // We need to setup the delegate environment so that it is
            // available when we process the Handler.
            LocationReferenceEnvironment delegateEnvironment = null;

            if (collectionType == ActivityCollectionType.Implementation)
            {
                delegateEnvironment = parent.ImplementationEnvironment;
            }
            else
            {
                delegateEnvironment = parent.PublicEnvironment;
            }

            if (this.RuntimeDelegateArguments.Count > 0)
            {
                ActivityLocationReferenceEnvironment newEnvironment = new ActivityLocationReferenceEnvironment(delegateEnvironment);
                delegateEnvironment = newEnvironment;

                for (int argumentIndex = 0; argumentIndex < this.RuntimeDelegateArguments.Count; argumentIndex++)
                {
                    RuntimeDelegateArgument runtimeDelegateArgument = this.RuntimeDelegateArguments[argumentIndex];
                    DelegateArgument delegateArgument = runtimeDelegateArgument.BoundArgument;

                    if (delegateArgument != null)
                    {
                        if (delegateArgument.Direction != runtimeDelegateArgument.Direction)
                        {
                            ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.RuntimeDelegateArgumentDirectionIncorrect, parent));
                        }

                        if (delegateArgument.Type != runtimeDelegateArgument.Type)
                        {
                            ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.RuntimeDelegateArgumentTypeIncorrect, parent));
                        }

                        // NOTE: We don't initialize this relationship here because
                        // at runtime we'll actually just place these variables in the
                        // environment of the Handler.  We'll initialize and set an
                        // ID when we process the Handler.
                        newEnvironment.Declare(delegateArgument, _owner, ref validationErrors);
                    }
                }
            }

            this.Environment = delegateEnvironment;

            if (this.Handler != null)
            {
                return this.Handler.InitializeRelationship(this, collectionType, ref validationErrors);
            }

            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDisplayName()
        {
            return _isDisplayNameSet;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
