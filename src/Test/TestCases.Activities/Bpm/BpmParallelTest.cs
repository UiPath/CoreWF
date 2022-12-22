﻿using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.CustomActivities;
using Xunit;
namespace TestCases.Activities.Bpm;
using Sequence = System.Activities.Statements.Sequence;
public class BpmParallelTest
{
    [Fact]
    public void Should_execute_branches()
    {
        var list = new List<string>();
        Variable<ICollection<string>> strings = new("strings", c => list);
        AddToCollection<string> branch1 = new() { Collection = strings, Item = "branch1" };
        AddToCollection<string> branch2 = new() { Collection = strings, Item = "branch2" };
        var parallel = new BpmParallel { Branches = { BpmStep.New(branch1), BpmStep.New(branch2) } };
        var flowchart = new BpmFlowchart { StartNode = parallel, Nodes = { parallel } };
        var root = new Sequence() { Variables = { strings }, Activities = { flowchart } };
        WorkflowInvoker.Invoke(root);
        list.ShouldBe(new() { "branch2", "branch1" });
    }
    [Fact]
    public void Should_join_branches()
    {
        var list = new List<string>();
        Variable<ICollection<string>> strings = new("strings", c => list);
        AddToCollection<string> branch1 = new() { Collection = strings, Item = "branch1" };
        AddToCollection<string> branch2 = new() { Collection = strings, Item = "branch2" };
        AddToCollection<string> item3 = new() { Collection = strings, Item = "item3" };
        var step3 = BpmStep.New(item3);
        BpmJoin join = new() { Next = step3 };
        var parallel = new BpmParallel { Branches = { BpmStep.New(branch1, join), BpmStep.New(branch2, join) } };
        join.Branches.AddRange(parallel.Branches);
        var flowchart = new BpmFlowchart { StartNode = parallel, Nodes = { parallel, join, step3 } };
        var root = new Sequence() { Variables = { strings }, Activities = { flowchart } };
        WorkflowInvoker.Invoke(root);
        list.ShouldBe(new() { "branch2", "branch1", "item3" });
    }
}