// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Security;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class FuncCompletionCallbackWrapper<T> : CompletionCallbackWrapper
    {
        private static readonly Type s_callbackType = typeof(CompletionCallback<T>);
        private static readonly Type[] s_callbackParameterTypes = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance), typeof(T) };

        private T _resultValue;

        public FuncCompletionCallbackWrapper(CompletionCallback<T> callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
            this.NeedsToGatherOutputs = true;
        }

        [DataMember(EmitDefaultValue = false, Name = "resultValue")]
        internal T SerializedResultValue
        {
            get { return _resultValue; }
            set { _resultValue = value; }
        }

        private int GetResultId(ActivityWithResult activity)
        {
            if (activity.Result != null)
            {
                return activity.Result.Id;
            }
            else
            {
                for (int i = 0; i < activity.RuntimeArguments.Count; i++)
                {
                    RuntimeArgument argument = activity.RuntimeArguments[i];

                    if (argument.IsResult)
                    {
                        return argument.Id;
                    }
                }
            }

            return -1;
        }

        protected override void GatherOutputs(ActivityInstance completedInstance)
        {
            int resultId = -1;

            if (completedInstance.Activity.HandlerOf != null)
            {
                DelegateOutArgument resultArgument = completedInstance.Activity.HandlerOf.GetResultArgument();
                if (resultArgument != null)
                {
                    resultId = resultArgument.Id;
                }
                else
                {
                    ActivityWithResult activity = completedInstance.Activity as ActivityWithResult;
                    // for auto-generated results, we should bind the value from the Handler if available
                    if (activity != null && TypeHelper.AreTypesCompatible(activity.ResultType, typeof(T)))
                    {
                        resultId = GetResultId(activity);
                    }
                }
            }
            else
            {
                Fx.Assert(completedInstance.Activity is ActivityWithResult, "should only be using FuncCompletionCallbackWrapper with ActivityFunc and ActivityWithResult");
                resultId = GetResultId((ActivityWithResult)completedInstance.Activity);
            }

            if (resultId >= 0)
            {
                Location location = completedInstance.Environment.GetSpecificLocation(resultId);
                Location<T> typedLocation = location as Location<T>;

                if (typedLocation != null)
                {
                    _resultValue = typedLocation.Value;
                }
                else if (location != null)
                {
                    _resultValue = TypeHelper.Convert<T>(location.Value);
                }
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
        {
            // Call the EnsureCallback overload that also looks for SomeMethod<T> where T is the result type
            // and the signature matches.
            EnsureCallback(s_callbackType, s_callbackParameterTypes, s_callbackParameterTypes[2]);
            CompletionCallback<T> completionCallback = (CompletionCallback<T>)this.Callback;
            completionCallback(context, completedInstance, _resultValue);
        }

        protected override void OnSerializingGenericCallback()
        {
            ValidateCallbackResolution(s_callbackType, s_callbackParameterTypes, s_callbackParameterTypes[2]);
        }
    }
}
