// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf;

using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.CustomActivities
{
    public sealed class NoPersistenceBlockActivity : CustomSequenceBase
    {
        public const string NoPersistenceBlockEntered = "Entered the NoPersistenceBlock";
        public const string NoPersistenceBlockExited = "Exited the NoPersistenceBlock";

        private Variable<NoPersistHandle> _noPersistHandle;

        public NoPersistenceBlockActivity()
        {
            _noPersistHandle = new Variable<NoPersistHandle>();
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            metadata.AddImplementationVariable(_noPersistHandle);
        }

        protected override void Execute(NativeActivityContext context)
        {
            TestTraceListenerExtension listenerExtension = context.GetExtension<TestTraceListenerExtension>();
            NoPersistHandle handle = _noPersistHandle.Get(context);
            handle.Enter(context);

            UserTrace.Trace(listenerExtension, context.WorkflowInstanceId, NoPersistenceBlockEntered);

            // Schedule all of the Sequence's Activities
            base.Execute(context);

            UserTrace.Trace(listenerExtension, context.WorkflowInstanceId, NoPersistenceBlockExited);
        }
    }
}
