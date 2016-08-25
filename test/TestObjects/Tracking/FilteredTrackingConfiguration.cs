// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Common.TestObjects.Tracking
{
    //provides additional properties to filter on say activity name, etc..
    [DataContract]
    public class FilteredTrackingConfiguration : TrackingConfiguration
    {
        public FilteredTrackingConfiguration()
        {
        }

        public List<string> ActivityNames
        {
            get;
            set;
        }

        public List<string> ActivityStates
        {
            get;
            set;
        }
    }
}
