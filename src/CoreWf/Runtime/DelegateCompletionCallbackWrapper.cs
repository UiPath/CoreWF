// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;

namespace CoreWf.Runtime
{
    [DataContract]
    internal class DelegateCompletionCallbackWrapper : CompletionCallbackWrapper
    {
        private static readonly Type s_callbackType = typeof(DelegateCompletionCallback);
        private static readonly Type[] s_callbackParameterTypes = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance), typeof(IDictionary<string, object>) };

        private Dictionary<string, object> _results;

        public DelegateCompletionCallbackWrapper(DelegateCompletionCallback callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
            this.NeedsToGatherOutputs = true;
        }

        [DataMember(EmitDefaultValue = false, Name = "results")]
        internal Dictionary<string, object> SerializedResults
        {
            get { return _results; }
            set { _results = value; }
        }

        protected override void GatherOutputs(ActivityInstance completedInstance)
        {
            if (completedInstance.Activity.HandlerOf != null)
            {
                IList<RuntimeDelegateArgument> runtimeArguments = completedInstance.Activity.HandlerOf.RuntimeDelegateArguments;
                LocationEnvironment environment = completedInstance.Environment;

                for (int i = 0; i < runtimeArguments.Count; i++)
                {
                    RuntimeDelegateArgument runtimeArgument = runtimeArguments[i];

                    if (runtimeArgument.BoundArgument != null)
                    {
                        if (ArgumentDirectionHelper.IsOut(runtimeArgument.Direction))
                        {
                            Location parameterLocation = environment.GetSpecificLocation(runtimeArgument.BoundArgument.Id);

                            if (parameterLocation != null)
                            {
                                if (_results == null)
                                {
                                    _results = new Dictionary<string, object>();
                                }

                                _results.Add(runtimeArgument.Name, parameterLocation.Value);
                            }
                        }
                    }
                }
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
        {
            EnsureCallback(s_callbackType, s_callbackParameterTypes);
            DelegateCompletionCallback completionCallback = (DelegateCompletionCallback)this.Callback;

            IDictionary<string, object> returnValue = _results;

            if (returnValue == null)
            {
                returnValue = ActivityUtilities.EmptyParameters;
            }

            completionCallback(context, completedInstance, returnValue);
        }
    }
}
