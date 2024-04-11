// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    public class StaticNodeStackInfo
    {
        private List<List<FlowSplit>> SplitsStack { get; } = new();

        public HashSet<FlowSplit> GetTop()
        {
            return new (SplitsStack.Select(b => b.LastOrDefault()).Where(b => b!= null));
        }

        public void Push(FlowSplit newBranch, StaticNodeStackInfo splitBranchInfo)
        {
            var result = splitBranchInfo.SplitsStack.Select(
                        stack => stack.Concat(new[] { newBranch }).ToList()
                    ).ToList();
            if (result.Count == 0)
                result.Add(new() { newBranch });
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
                if (HasBranch(pre))
                    continue;
                SplitsStack.Add(pre);
            }
        }

        public void AddPop(StaticNodeStackInfo popFrom)
        {
            var result = popFrom.SplitsStack.Select(s => Enumerable.Reverse(s).Skip(1).Reverse().ToList()).ToList();
            AddStack(result);
        }

        private bool HasBranch(List<FlowSplit> branch)
            => SplitsStack.Any(b => branch.Count == b.Count && branch.All(p => b.Contains(p)));

        internal bool IsOnBranch(StaticNodeStackInfo toConfirm)
        {
            foreach (var branchToConfirm in toConfirm.SplitsStack)
            {
                if (HasBranch(branchToConfirm))
                    return true;
            }
            return false;
        }
    }
}
