// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace LegacyTest.Test.Common.TestObjects.Tracking
{
    //provides additional properties specific ot the oob sql tracking participant.
    [DataContract]
    public class SqlTrackingConfiguration : TrackingConfiguration
    {
        public SqlTrackingConfiguration()
        {
        }

        public string ProfileName
        {
            get;
            set;
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public bool IsTransactional
        {
            get;
            set;
        }

        public bool PushToTrackingDataManager
        {
            get;
            set;
        }
    }
}
