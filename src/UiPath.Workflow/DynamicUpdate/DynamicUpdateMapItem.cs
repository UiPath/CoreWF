// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.DynamicUpdate
{
    using System;
    using System.Activities.XamlIntegration;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    
    [TypeConverter(typeof(DynamicUpdateMapItemConverter))]
    [DataContract]
    public class DynamicUpdateMapItem
    {
        internal DynamicUpdateMapItem(int originalId)
        {
            this.OriginalId = originalId;
        }

        internal DynamicUpdateMapItem(int originalVariableOwnerId, int originalVariableId)
        {
            this.OriginalVariableOwnerId = originalVariableOwnerId;
            this.OriginalId = originalVariableId;
        }

        [DataMember]
        internal int OriginalId
        {
            get;
            set;
        }

        [DataMember]
        internal int OriginalVariableOwnerId
        {
            get;
            set;
        }

        internal bool IsVariableMapItem
        {
            get
            {
                return this.OriginalVariableOwnerId > 0;
            }
        }
    }
}

