// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;
using SplitsStack = List<FlowSplit>;

partial class Flowchart
{
    private sealed class StaticNodeStackInfo
    {
        private List<SplitsStack> SplitsStacks { get; init; } = new();
        public static StaticNodeStackInfo EmptyStack { get; } = new() { SplitsStacks = new() { new() } };

        public HashSet<FlowSplit> GetTop()
            => new (SplitsStacks.Select(b => b.LastOrDefault()));

        public void AddPush(FlowSplit newSplit, StaticNodeStackInfo splitsStackInfo)
            => AddUniqueStack(splitsStackInfo.SplitsStacks
                .Select(stack => stack.Concat(new[] { newSplit }).ToList()));
        public void AddPop(StaticNodeStackInfo popFrom)
            => AddUniqueStack(popFrom.SplitsStacks
                .Select(s => new SplitsStack(s.Take(s.Count - 1))));

        public void PropagateStack(StaticNodeStackInfo preStacks)
            => AddUniqueStack(preStacks.SplitsStacks);

        private void AddUniqueStack(IEnumerable<SplitsStack> stack)
        {
            foreach (var pre in stack)
            {
                if (HasStack(pre))
                    continue;
                SplitsStacks.Add(pre);
            }
        }

        private bool HasStack(SplitsStack stack)
            => SplitsStacks.Any(s => s.SequenceEqual(stack));
    }
}
