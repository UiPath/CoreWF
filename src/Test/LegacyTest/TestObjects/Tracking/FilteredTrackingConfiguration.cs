// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LegacyTest.Test.Common.TestObjects.Tracking
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
