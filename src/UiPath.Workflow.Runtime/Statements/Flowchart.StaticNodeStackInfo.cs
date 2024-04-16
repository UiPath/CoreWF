// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    private sealed class StaticNodeStackInfo
    {
        private List<FlowSplit[]> SplitsStacks { get; init; } = new();
        public static StaticNodeStackInfo EmptyStack { get; } = new() { SplitsStacks = [[]] };

        public HashSet<FlowSplit> GetTop()
            => new (SplitsStacks.Select(b => b.LastOrDefault()));

        public void AddPush(FlowSplit newSplit, StaticNodeStackInfo splitsStackInfo)
            => AddUniqueStack(splitsStackInfo.SplitsStacks
                .Select<FlowSplit[], FlowSplit[]>(stack => [.. stack, newSplit]));
        public void AddPop(StaticNodeStackInfo popFrom)
            => AddUniqueStack(popFrom.SplitsStacks
                .Select<FlowSplit[], FlowSplit[]>(s => [..s.Take(s.Length - 1)]));

        public void PropagateStack(StaticNodeStackInfo preStacks)
            => AddUniqueStack(preStacks.SplitsStacks);

        private void AddUniqueStack(IEnumerable<FlowSplit[]> stack)
        {
            foreach (var pre in stack)
            {
                if (HasStack(pre))
                    continue;
                SplitsStacks.Add(pre);
            }
        }

        private bool HasStack(FlowSplit[] stack)
            => SplitsStacks.Any(s => s.SequenceEqual(stack));
    }
}
