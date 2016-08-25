// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.CoreWf.Tracking
{
    //[ContentProperty("Queries")]
    public class TrackingProfile
    {
        private Collection<TrackingQuery> _queries;

        public TrackingProfile()
        {
        }

        [DefaultValue(null)]
        public string Name { get; set; }

        [DefaultValue(ImplementationVisibility.RootScope)]
        public ImplementationVisibility ImplementationVisibility { get; set; }

        [DefaultValue(null)]
        public string ActivityDefinitionId { get; set; }

        public Collection<TrackingQuery> Queries
        {
            get
            {
                if (_queries == null)
                {
                    _queries = new Collection<TrackingQuery>();
                }
                return _queries;
            }
        }
    }
}
