using Shouldly;
using System.Activities;
using System.Activities.Statements;
using Xunit;
using System.Activities.Expressions;
using TestObjects.XamlTestDriver;
using System.IO;
using System;
using System.Diagnostics;
namespace TestCases.Activitiess.Bpm;

public class SplitAndMergeTests
{
    [Fact]
    public void RoundTrip_xaml()
    {
        var outersplit = new FlowSplit() { DisplayName = "OuterSplit" };
        var outerMerge = new FlowMerge() { DisplayName = "OuterMerge" }.Text("stop");
        var merge = new FlowMerge().FlowTo(outerMerge);
        var split = new FlowSplit()
            .AddBranches(
                TestFlow.Text("branch1").FlowTo(merge),
                TestFlow.Text("branch2").FlowTo(merge));
        outersplit.AddBranches(split);
        var flowchart = new Flowchart { StartNode = outersplit };
        var roundTrip = XamlRoundTrip(flowchart);
        TestFlow.Results(roundTrip)
            .ShouldBe(["branch1", "branch2", "stop"]);

        T XamlRoundTrip<T>(T obj)
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\testFlowchart.xaml";
            File.Delete(filePath);
            using (var stream = File.OpenWrite(filePath))
            {
                XamlTestDriver.Serialize(obj, stream);
            }
            var asm = GetType().Assembly;
            var sampleStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.TestXamls.{"testFlowchart"}.xaml");
            using var sampleReader = new StreamReader(sampleStream);
            var sampleXaml = sampleReader.ReadToEnd();
            var currentXaml = File.ReadAllText(filePath);
            currentXaml.ShouldBe(sampleXaml);

            using (var stream = File.OpenRead(filePath))
            {
                return (T)XamlTestDriver.Deserialize(stream);
            }
        }
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_split_without_merge.png" scale="0.15" />
    public void Invalid_split_without_merge()
    {
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1"),
                TestFlow.Text("branch2")
            }
        };
        TestFlow.Validate(split)
            .HasErrorFor(split);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_merge_without_split.png" scale="0.15" />
    public void Invalid_merge_without_split()
    {
        var merge = new FlowMerge().Text("stop");
        TestFlow.Validate(TestFlow.Text("start").FlowTo(merge))
            .HasErrorFor(merge);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_shared_merge.png" scale="0.15" />
    public void Invalid_shared_merge()
    {
        var sharedMerge = new FlowMerge().Text("stop");
        var innerSplit = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1Inner").FlowTo(sharedMerge),
                TestFlow.Text("branch2Inner").FlowTo(sharedMerge)
            }
        };
        var outerSplit = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1Outer").FlowTo(innerSplit),
                TestFlow.Text("branch2Outer").FlowTo(sharedMerge)
            }
        };

        TestFlow.Validate(outerSplit)
            .HasErrorFor(sharedMerge);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_split_different_merges.png" scale="0.15" />
    public void Invalid_split_different_merges()
    {
        var stop = TestFlow.Text("stop");
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1").FlowTo(new FlowMerge().FlowTo(stop)),
                TestFlow.Text("branch2").FlowTo(new FlowMerge().FlowTo(stop))
            }
        };

        TestFlow.Validate(split)
            .HasErrorFor(split);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_node_split_stack_case1.png" scale="0.15" />
    public void Invalid_node_split_stack_case1()
    {
        var outerMerge = new FlowMerge().Text("stop");
        var branchInner = TestFlow.Text("branchInner").FlowTo(new FlowMerge().FlowTo(outerMerge));
        var innerSplit = new FlowSplit().AddBranches(branchInner);

        var outerSplit = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1Outer")
                    .FlowTo(new FlowDecision()
                    {
                        Condition = new Literal<bool>(false),
                        True = innerSplit,
                        False = branchInner
                    }),
                TestFlow.Text("branch2Outer").FlowTo(outerMerge)
            }
        };

        TestFlow.Validate(outerSplit)
            .HasErrorFor(branchInner);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_node_split_stack_case2.png" scale="0.15" />
    public void Invalid_node_split_stack_case2()
    {
        var stop = TestFlow.Text("stop");
        var merge = new FlowMerge().FlowTo(stop);
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1")
                    .FlowTo(new FlowDecision()
                    {
                        Condition = new Literal<bool>(false),
                        True = merge,
                        False = stop
                    }),
                TestFlow.Text("branch2").FlowTo(merge)
            }
        };

        TestFlow.Validate(split)
            .HasErrorFor(stop);
    }

    [Fact]
    /// <image url="./Diagrams/Invalid_node_split_stack_case3.png" scale="0.15" />
    public void Invalid_node_split_stack_case3()
    {
        var merge = new FlowMerge().Text("stop");
        var branch1 = TestFlow.Text("branch1");
        var split = new FlowSplit()
        {
            Branches = {
                branch1,
                TestFlow.Text("branch2").FlowTo(merge)
            }
        };
        branch1.FlowTo(
            new FlowDecision()
            {
                Condition = new Literal<bool>(false),
                True = merge,
                False = split
            });

        TestFlow.Validate(split)
            .HasErrorFor(split);
    }

    [Fact]
    /// <image url="./Diagrams/Should_join_branches.png" scale="0.15" />
    public void Should_join_branches()
    {
        var merge = new FlowMerge().Text("stop");

        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1").FlowTo(merge),
                TestFlow.Text("branch2").FlowTo(merge)
            }
        };

        TestFlow.Results(split)
            .ShouldBe(["branch1", "branch2", "stop"]);
    }

    [Fact]
    /// <image url="./Diagrams/Should_join_branches_with_inner_split.png" scale="0.15" />
    public void Should_join_branches_with_inner_split()
    {
        var outerMerge = new FlowMerge().Text("stop");
        var innerMerge = new FlowMerge().Text("innerMerged").FlowTo(outerMerge);
        var innerSplit = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1Inner").FlowTo(innerMerge),
                TestFlow.Text("branch2Inner").FlowTo(innerMerge)
            }
        };
        var outerSplit = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1Outer").FlowTo(innerSplit),
                TestFlow.Text("branch2Outer").FlowTo(outerMerge)
            }
        };

        TestFlow.Results(outerSplit)
            .ShouldBe(["branch1Outer", "branch2Outer", "branch1Inner", "branch2Inner", "innerMerged", "stop"]);
    }


    [Fact]
    /// <image url="./Diagrams/Should_join_with_skiped_branches.png" scale="0.15" />
    public void Should_join_with_skiped_branches()
    {
        var merge = new FlowMerge().Text("stop");
        var skippedBranch = TestFlow.Text("executedPart")
            .FlowTo(new FlowDecision()
            {
                Condition = new Literal<bool>(false),
                True = TestFlow.Text("skippedPart").FlowTo(merge)
            });
        var split = new FlowSplit()
        {
            Branches = {
                skippedBranch ,
                TestFlow.Text("executedBranch").FlowTo(merge)
            }
        };

        TestFlow.Results(split)
            .ShouldBe(["executedPart", "executedBranch", "stop"]);
    }

    [Fact]
    /// <image url="./Diagrams/Should_join_with_shared_branch.png" scale="0.15" />
    public void Should_join_with_shared_branch()
    {
        var merge = new FlowMerge().Text("stop");
        var sharedPart = TestFlow.Text("sharedPart")
            .Text("sharedPart2")
            .FlowTo(merge);
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow
                    .Text("branch1")
                    .FlowTo(sharedPart),
                TestFlow
                    .Text("branch2")
                    .FlowTo(new FlowDecision()
                    {
                        Condition = new Literal<bool>(false),
                        True = TestFlow.Text("skippedPart").FlowTo(merge),
                        False = sharedPart
                    })
            }
        };

        TestFlow.Results(split)
            .ShouldBe(["branch1", "branch2", "sharedPart", "sharedPart", "sharedPart2", "sharedPart2", "stop"]);
    }

    private void Merge_executed_at_least_one(MergeBehavior mergeBehavior)
    {
        var merge = new FlowMerge() { Behavior = mergeBehavior }.Text("stop");

        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1")
                    .FlowTo(new FlowDecision()
                    {
                        Condition = new Literal<bool>(false),
                        True = merge,
                    }),
                TestFlow.Text("branch2")
                    .FlowTo(new FlowDecision()
                    {
                        Condition = new Literal<bool>(false),
                        True = merge,
                    })
            }
        };

        TestFlow.Results(split)
            .ShouldBe(["branch1", "branch2"]);
    }

    [Fact]
    /// <image url="./Diagrams/MergeAll_executed_at_least_one.png" scale="0.15" />
    public void MergeAll_executed_at_least_one() =>
        Merge_executed_at_least_one(new MergeAllBehavior());

    [Fact]
    /// <image url="./Diagrams/MergeAny_executed_at_least_one.png" scale="0.15" />
    public void MergeAny_executed_at_least_one() =>
        Merge_executed_at_least_one(new MergeFirstBehavior());


    [Fact]
    /// <image url="./Diagrams/MergeAny_cancels_unconnected.png" scale="0.15" />
    public void MergeAny_cancels_unconnected()
    {
        var merge = new FlowMerge() { Behavior = new MergeFirstBehavior() }.Text("stop");
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow
                    .Text("branch1")
                    .Text("branch1")
                    .FlowTo(merge),
                TestFlow
                    .Text("branch2")
                    .CancelableText(TimeSpan.FromSeconds(5), textOnCancel: "longDelay canceled")
                    .Text("delayedBranch")
            }
        };

        TestFlow.Results(split)
            .ShouldBe(["branch1", "branch2", "branch1", "longDelay canceled", "stop"]);
    }

    [Fact]
    /// <image url="./Diagrams/MergeAny_continues_after_first_and_cancels_the_rest.png" scale="0.15" />
    public void MergeAny_continues_after_first_and_cancels_the_rest()
    {
        var merge = new FlowMerge() { Behavior = new MergeFirstBehavior() }.Text("stop");
        var nonCancelableDelay = TimeSpan.FromMilliseconds(200);
        var cancelableDelay = nonCancelableDelay + TimeSpan.FromSeconds(2);
        var split = new FlowSplit()
        {
            Branches = {
                TestFlow.Text("branch1")
                    .DelayedText(nonCancelableDelay / 4, "branch1 delayed")
                    .FlowTo(merge),
                TestFlow
                    .Text("branch2")
                    .DelayedText(nonCancelableDelay, "branch2 delayed")
                    .Text("NodeIsNotExecuted")
                    .FlowTo(merge),
                TestFlow
                    .Text("branch3")
                    .CancelableText(cancelableDelay, "branch3 canceled")
                    .Text("NodeIsNotExecuted2")
                    .FlowTo(merge)
            }
        };

        var sw = Stopwatch.StartNew();
        var results = TestFlow.Results(split);
        sw.Stop();
        sw.Elapsed.ShouldBeInRange(nonCancelableDelay, cancelableDelay);
        results.ShouldBe(["branch1", "branch2", "branch3", "branch1 delayed", "branch3 canceled", "branch2 delayed", "stop"]);
    }

    [Fact]
    public void Should_persist_join()
    {
        var merge = new FlowMerge().Text("stop");
        var split = new FlowSplit().AddBranches(
            TestFlow
                .Text("branch1")
                .FlowTo(new FlowStep())
                .FlowTo(merge),
            TestFlow
                .Text("branch2")
                .FlowTo(new FlowStep())
                .FlowTo(merge),
            TestFlow
                .Text("branch3")
                .FlowTo(new BlockingActivity())
                .Text("blockingContinuation")
                .FlowTo(merge));
        TestFlow.Results(split)
            .ShouldBe(["branch1", "branch2", "branch3", "blockingContinuation", "stop"]);
    }
}

public class BlockingActivity : NativeActivity
{

    public BlockingActivity()
    {
        DisplayName = WorkflowApplicationTestExtensions.WorkflowApplicationTestExtensions.AutoResumedBookmarkNamePrefix + "blocking";
    }


    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // nothing to do
    }

    protected override void Execute(NativeActivityContext context)
    {
        context.CreateBookmark(DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    protected override void Cancel(NativeActivityContext context)
    {
        base.Cancel(context);
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
    }

    protected override bool CanInduceIdle => true;
}