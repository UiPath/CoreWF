// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Statements
{

    internal sealed class DefaultCompensation : NativeActivity
    {
        private Activity _body;

        private Variable<CompensationToken> _toCompensateToken;

        private CompletionCallback _onChildCompensated;

        public DefaultCompensation()
            : base()
        {
            _toCompensateToken = new Variable<CompensationToken>();

            _body = new InternalCompensate()
            {
                Target = new InArgument<CompensationToken>(_toCompensateToken),
            };
        }

        public InArgument<CompensationToken> Target
        {
            get;
            set;
        }

        private Activity Body
        {
            get { return _body; }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument targetArgument = new RuntimeArgument("Target", typeof(CompensationToken), ArgumentDirection.In);
            metadata.Bind(this.Target, targetArgument);

            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { targetArgument });

            metadata.SetImplementationVariablesCollection(new Collection<Variable> { _toCompensateToken });

            Fx.Assert(this.Body != null, "Body must be valid");
            metadata.SetImplementationChildrenCollection(new Collection<Activity> { this.Body });
        }

        protected override void Execute(NativeActivityContext context)
        {
            InternalExecute(context, null);
        }

        private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
            if (compensationExtension == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CompensateWithoutCompensableActivity(this.DisplayName)));
            }

            CompensationToken token = Target.Get(context);
            CompensationTokenData tokenData = token == null ? null : compensationExtension.Get(token.CompensationId);

            Fx.Assert(tokenData != null, "CompensationTokenData must be valid");

            if (tokenData.ExecutionTracker.Count > 0)
            {
                if (_onChildCompensated == null)
                {
                    _onChildCompensated = new CompletionCallback(InternalExecute);
                }

                _toCompensateToken.Set(context, new CompensationToken(tokenData.ExecutionTracker.Get()));

                Fx.Assert(Body != null, "Body must be valid");
                context.ScheduleActivity(Body, _onChildCompensated);
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            // Suppress Cancel   
        }
    }
}
