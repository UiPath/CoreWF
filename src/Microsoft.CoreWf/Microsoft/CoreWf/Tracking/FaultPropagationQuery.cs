// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf.Tracking
{
    public sealed class FaultPropagationQuery : TrackingQuery
    {
        public FaultPropagationQuery()
        {
            this.FaultSourceActivityName = "*";
            this.FaultHandlerActivityName = "*";
        }

        public string FaultHandlerActivityName { get; set; }

        public string FaultSourceActivityName { get; set; }
    }
}
