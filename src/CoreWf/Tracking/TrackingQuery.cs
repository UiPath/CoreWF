// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

public abstract class TrackingQuery
{
    private IDictionary<string, string> _queryAnnotations;

    protected TrackingQuery() { }

    public IDictionary<string, string> QueryAnnotations
    {
        get
        {
            _queryAnnotations ??= new Dictionary<string, string>();
            return _queryAnnotations;
        }
    }

    internal bool HasAnnotations => _queryAnnotations != null && _queryAnnotations.Count > 0;
}
