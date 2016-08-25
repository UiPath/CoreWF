// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Test.Common.TestObjects.Tracking
{
    [DataContract]
    public class TrackingConfiguration //: TestConfigurationObject
    {
        public TrackingConfiguration()
        {
            if (string.IsNullOrEmpty(this.TrackingParticipantName))
            {
                this.TrackingParticipantName = Guid.NewGuid().ToString();
            }
            isExecutingRecExpectedOnFaultedState = true;
        }

        public bool isExecutingRecExpectedOnFaultedState { get; set; }

        public string TrackingParticipantName
        {
            get;
            set;
        }

        public TestProfileType TestProfileType
        {
            get;
            set;
        }

        public TrackingParticipantType TrackingParticipantType
        {
            get;
            set;
        }

        public ProfileManagerType ProfileManagerType
        {
            get;
            set;
        }
    }
}
