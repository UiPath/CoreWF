// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Test.Common.TestObjects.Tracking
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
