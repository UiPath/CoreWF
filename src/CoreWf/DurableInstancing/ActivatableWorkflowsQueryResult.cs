// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace System.Activities.DurableInstancing;

[Fx.Tag.XamlVisible(false)]
public sealed class ActivatableWorkflowsQueryResult : InstanceStoreQueryResult
{
    private static readonly ReadOnlyDictionary<XName, object> emptyDictionary = new(new Dictionary<XName, object>(0));

    public ActivatableWorkflowsQueryResult()
    {
        ActivationParameters = new List<IDictionary<XName, object>>(0);
    }

    public ActivatableWorkflowsQueryResult(IDictionary<XName, object> parameters)
    {
        ActivationParameters = new List<IDictionary<XName, object>>
            { parameters == null ? emptyDictionary : new ReadOnlyDictionary<XName, object>(new Dictionary<XName, object>(parameters)) };
    }

    public ActivatableWorkflowsQueryResult(IEnumerable<IDictionary<XName, object>> parameters)
    {
        if (parameters == null)
        {
            ActivationParameters = new List<IDictionary<XName, object>>(0);
        }
        else
        {
            ActivationParameters = new List<IDictionary<XName, object>>(parameters.Select(dictionary =>
                dictionary == null ? emptyDictionary : new ReadOnlyDictionary<XName, object>(new Dictionary<XName, object>(dictionary))));
        }
    }

    public List<IDictionary<XName, object>> ActivationParameters { get; private set; }
}
