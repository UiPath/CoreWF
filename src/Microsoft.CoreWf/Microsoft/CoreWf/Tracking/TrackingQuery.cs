// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace CoreWf.Tracking
{
    public abstract class TrackingQuery
    {
        private IDictionary<string, string> _queryAnnotations;

        protected TrackingQuery()
        {
        }

        public IDictionary<string, string> QueryAnnotations
        {
            get
            {
                if (_queryAnnotations == null)
                {
                    _queryAnnotations = new Dictionary<string, string>();
                }
                return _queryAnnotations;
            }
        }

        internal bool HasAnnotations
        {
            get
            {
                return _queryAnnotations != null && _queryAnnotations.Count > 0;
            }
        }
    }
}
