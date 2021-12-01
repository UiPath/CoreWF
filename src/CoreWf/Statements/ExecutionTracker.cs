// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

[DataContract]
internal sealed class ExecutionTracker
{
    private List<CompensationTokenData> _executionOrderedList;

    public ExecutionTracker()
    {
        _executionOrderedList = new List<CompensationTokenData>();
    }

    public int Count => _executionOrderedList.Count;

    [DataMember(Name = "executionOrderedList")]
    internal List<CompensationTokenData> SerializedExecutionOrderedList
    {
        get => _executionOrderedList;
        set => _executionOrderedList = value;
    }

    public void Add(CompensationTokenData compensationToken) => _executionOrderedList.Insert(0, compensationToken);

    public void Remove(CompensationTokenData compensationToken) => _executionOrderedList.Remove(compensationToken);

    public CompensationTokenData Get() => Count > 0 ? _executionOrderedList[0] : null;
}
