// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CoreWf.Tracking
{
    public class CustomTrackingQuery : TrackingQuery
    {
        public CustomTrackingQuery()
        {
        }

        public string Name
        {
            get;
            set;
        }

        public string ActivityName
        {
            get;
            set;
        }
    }
}
