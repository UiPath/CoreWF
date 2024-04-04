// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    public class StaticBranchInfo
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
                return _branches;
            }
        }
        private readonly HashSet<(StaticBranchInfo, FlowSplitBranch)> _pushes = new();
        private readonly HashSet<StaticBranchInfo> _inheritedStacks = new();
        private bool _hasPop = false;

        public HashSet<FlowSplitBranch> GetTop()
        {
            return new (Branches.Select(b => b.LastOrDefault()).Where(b => b!= null));
        }

        public void Push(FlowSplitBranch newBranch, StaticBranchInfo splitBranchInfo)
        {
            _pushes.Add(new(splitBranchInfo, newBranch));
        }

        public void PropagateStack(StaticBranchInfo preStacks)
        {
            _inheritedStacks.Add(preStacks);
        }

        private void AddStack(List<List<FlowSplitBranch>> stack)
        {
            foreach (var pre in stack)
            {
                AddStack(pre);
            }

            void AddStack(IEnumerable<FlowSplitBranch> stack)
            {
                var pre = _hasPop 
                        ? stack.Reverse().Skip(1).Reverse().ToList()
                        : stack.ToList();
                if (HasBranch(pre))
                    return;

                _branches.Add(pre);
            }
        }

        public void AddPop()
        {
            _hasPop = true;
        }

        private bool HasBranch(List<FlowSplitBranch> branch)
            => Branches.Any(b => branch.Count == b.Count && branch.All(p => b.Contains(p)));

        internal bool IsOnBranch(StaticBranchInfo toConfirm)
        {
            foreach (var branchToConfirm in toConfirm.Branches)
            {
                if (HasBranch(branchToConfirm))
                    return true;
            }
            return false;
        }

        public StaticBranchInfo() { }
    }
}
