// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    public class StaticNodeBranchInfo
    {
        List<List<FlowSplit>> _branches;
        private List<List<FlowSplit>> SplitsStack
        {
            get
            {
                if (_branches != null)
                    return _branches;
                _branches = new List<List<FlowSplit>>();
                foreach(var inheritedStack in _inheritedStacks) 
                {
                    AddStack(inheritedStack.SplitsStack);
                }
                foreach(var push in _pushes)
                {
                    var result = push.Item1.SplitsStack.Select(
                                stack => stack.Concat(new[] { push.Item2 }).ToList()
                            ).ToList();
                    if (result.Count == 0)
                        result.Add(new() { push.Item2 });
                    AddStack(result);
                }
                foreach (var pop in _pops)
                {
                    var result = pop.SplitsStack.Select(s => Enumerable.Reverse(s).Skip(1).Reverse().ToList()).ToList();
                    AddStack(result);
                }
                return _branches;
            }
        }
        private readonly HashSet<(StaticNodeBranchInfo, FlowSplit)> _pushes = new();
        private readonly HashSet<StaticNodeBranchInfo> _inheritedStacks = new();
        private readonly HashSet<StaticNodeBranchInfo> _pops = new();

        public HashSet<FlowSplit> GetTop()
        {
            return new (SplitsStack.Select(b => b.LastOrDefault()).Where(b => b!= null));
        }

        public void Push(FlowSplit newBranch, StaticNodeBranchInfo splitBranchInfo)
        {
            _pushes.Add(new(splitBranchInfo, newBranch));
        }

        public void PropagateStack(StaticNodeBranchInfo preStacks)
        {
            _inheritedStacks.Add(preStacks);
        }

        private void AddStack(List<List<FlowSplit>> stack)
        {
            foreach (var pre in stack)
            {
                if (HasBranch(pre))
                    continue;
                _branches.Add(pre);
            }
        }

        public void AddPop(StaticNodeBranchInfo popFrom)
        {
            _pops.Add(popFrom);
        }

        private bool HasBranch(List<FlowSplit> branch)
            => SplitsStack.Any(b => branch.Count == b.Count && branch.All(p => b.Contains(p)));

        internal bool IsOnBranch(StaticNodeBranchInfo toConfirm)
        {
            foreach (var branchToConfirm in toConfirm.SplitsStack)
            {
                if (HasBranch(branchToConfirm))
                    return true;
            }
            return false;
        }

        public StaticNodeBranchInfo() { }
    }
}
