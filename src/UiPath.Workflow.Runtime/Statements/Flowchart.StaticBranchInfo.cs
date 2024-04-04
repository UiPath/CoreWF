// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    public class StaticNodeBranchInfo
    {
        List<List<FlowSplitBranch>> _branches;
        private List<List<FlowSplitBranch>> Branches
        {
            get
            {
                if (_branches != null)
                    return _branches;
                _branches = new List<List<FlowSplitBranch>>();
                foreach(var inheritedStack in _inheritedStacks) 
                {
                    AddStack(inheritedStack.Branches);
                }
                foreach(var push in _pushes)
                {
                    var result = push.Item1.Branches.Select(
                                stack => stack.Concat(new[] { push.Item2 }).ToList()
                            ).ToList();
                    if (result.Count == 0)
                        result.Add(new() { push.Item2 });
                    AddStack(result);
                }
                foreach (var pop in _pops)
                {
                    var result = pop.Branches.Select(s => Enumerable.Reverse(s).Skip(1).Reverse().ToList()).ToList();
                    AddStack(result);
                }
                return _branches;
            }
        }
        private readonly HashSet<(StaticNodeBranchInfo, FlowSplitBranch)> _pushes = new();
        private readonly HashSet<StaticNodeBranchInfo> _inheritedStacks = new();
        private readonly HashSet<StaticNodeBranchInfo> _pops = new();

        public HashSet<FlowSplitBranch> GetTop()
        {
            return new (Branches.Select(b => b.LastOrDefault()).Where(b => b!= null));
        }

        public void Push(FlowSplitBranch newBranch, StaticNodeBranchInfo splitBranchInfo)
        {
            _pushes.Add(new(splitBranchInfo, newBranch));
        }

        public void PropagateStack(StaticNodeBranchInfo preStacks)
        {
            _inheritedStacks.Add(preStacks);
        }

        private void AddStack(List<List<FlowSplitBranch>> stack)
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

        private bool HasBranch(List<FlowSplitBranch> branch)
            => Branches.Any(b => branch.Count == b.Count && branch.All(p => b.Contains(p)));

        internal bool IsOnBranch(StaticNodeBranchInfo toConfirm)
        {
            foreach (var branchToConfirm in toConfirm.Branches)
            {
                if (HasBranch(branchToConfirm))
                    return true;
            }
            return false;
        }

        public StaticNodeBranchInfo() { }
    }
}
