// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    private sealed class StaticNodeStackInfo
    {
        private List<List<FlowSplit>> SplitsStack { get; init; } = new();
        public static StaticNodeStackInfo EmptyStack { get; } = new StaticNodeStackInfo(){SplitsStack = new() { new() } };

        public HashSet<FlowSplit> GetTop()
        {
            return new (SplitsStack.Select(b => b.LastOrDefault()));
        }

        public void Push(FlowSplit newSplit, StaticNodeStackInfo splitsStackInfo)
        {
            var result = splitsStackInfo.SplitsStack.Select(
                        stack => stack.Concat(new[] { newSplit }).ToList()
                    ).ToList();
            AddStack(result);
        }

        public void PropagateStack(StaticNodeStackInfo preStacks)
        {
            AddStack(preStacks.SplitsStack);
        }

        private void AddStack(List<List<FlowSplit>> stack)
        {
            foreach (var pre in stack)
            {
                if (HasStack(pre))
                    continue;
                SplitsStack.Add(pre);
            }
        }

        public void AddPop(StaticNodeStackInfo popFrom)
        {
            var result = popFrom.SplitsStack.Select(s => Enumerable.Reverse(s).Skip(1).Reverse().ToList()).ToList();
            AddStack(result);
        }

        private bool HasStack(List<FlowSplit> stack)
            => SplitsStack.Any(b => stack.Count == b.Count && stack.All(p => b.Contains(p)));

        internal bool IsOnStack(StaticNodeStackInfo toConfirm)
        {
            foreach (var stackToConfirm in toConfirm.SplitsStack)
            {
                if (HasStack(stackToConfirm))
                    return true;
            }
            return false;
        }
    }
}
