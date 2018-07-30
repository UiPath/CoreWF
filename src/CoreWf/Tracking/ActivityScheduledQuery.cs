// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Tracking
{
    public sealed class ActivityScheduledQuery : TrackingQuery
    {
        public ActivityScheduledQuery()
        {
            this.ActivityName = "*";
            this.ChildActivityName = "*";
        }

        public string ActivityName { get; set; }
        public string ChildActivityName { get; set; }

    }
}
