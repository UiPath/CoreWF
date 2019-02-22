// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking
{
    using System.Collections.Generic;

    public abstract class TrackingQuery
    {
        private IDictionary<string, string> queryAnnotations;

        protected TrackingQuery()
        {
        }

        public IDictionary<string, string> QueryAnnotations
        {
            get
            {
                if (this.queryAnnotations == null)
                {
                    this.queryAnnotations = new Dictionary<string, string>();
                }
                return this.queryAnnotations;
            }
        }

        internal bool HasAnnotations
        {
            get
            {
                return this.queryAnnotations != null && this.queryAnnotations.Count > 0;
            }
        }
    }
}
