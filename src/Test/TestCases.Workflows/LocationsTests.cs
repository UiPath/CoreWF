using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Linq;
using Xunit;

namespace TestCases.Workflows
{
    public class LocationsTests
    {
        private static readonly Func<LocationReference, bool> IsCompatible = l => l.Type == typeof(string);
        private static readonly Func<RuntimeArgument, bool> ArgumentsFilter = arg => arg.Direction != ArgumentDirection.In && arg.BoundArgument.Expression == null;
        [Fact]
        public void SimpleWorkflowWithArgsAndVar()
        {
            var sequence = Load(TestXamls.SimpleWorkflowWithArgsAndVar);
            var writeLine1 = (WriteLine)sequence.Activities.Last();
            var to2 = sequence.Activities[1].RuntimeArguments[1];
            var myVar = sequence.Variables[0];

            var locations = ScopeUtils.GetCompatibleLocations(writeLine1, IsCompatible, ArgumentsFilter);

            var locals = new LocationReference[] { myVar }.Concat(sequence.Parent.RuntimeArguments);
            var arguments = new[] { new ReachableArgument(to2, to2.Owner, sequence) };
            locations.Locals.ShouldBe(locals);
            locations.ReachableArguments.ShouldBe(arguments);

            var scope = (ScopeActivity)sequence.Activities[2];

            locations = ScopeUtils.GetCompatibleLocations(scope.Body.Handler, IsCompatible, ArgumentsFilter);
            locations.Locals.ShouldBe(locals);
            locations.ReachableArguments.ShouldBe(arguments);
        }
        Sequence Load(TestXamls xaml)
        {
            var root = TestHelper.GetActivityFromXamlResource(xaml);
            var results = ActivityValidationServices.Validate(root);
            return (Sequence)root.ImplementationChildren[0];
        }
        [Fact]
        public void NestedSequencesWithVars()
        {
            var sequence = Load(TestXamls.NestedSequencesWithVars);
            var sequence2 = (Sequence)sequence.Activities[1];
            var writeLine2 = (WriteLine)sequence2.Children.Last();
            var myVar2 = sequence2.Variables.Single(v => v.Name == "MyVar2");
            var to = sequence2.Activities[0].RuntimeArguments[1];

            var locations = ScopeUtils.GetCompatibleLocations(writeLine2, IsCompatible, ArgumentsFilter);

            locations.Locals.ShouldBe(new[] { myVar2 });
            locations.ReachableArguments.ShouldBe(new[] { new ReachableArgument(to, to.Owner, sequence2) });

            var sequence1 = (Sequence)sequence.Activities[0];
            var writeLine1 = (WriteLine)sequence1.Activities.Last();
            var myVar = sequence1.Variables.Single(v => v.Name == "MyVar");
            var to2 = sequence1.Activities[1].RuntimeArguments[1];

            locations = ScopeUtils.GetCompatibleLocations(writeLine1, IsCompatible, ArgumentsFilter);

            locations.Locals.ShouldBe(new[] { myVar });
            locations.ReachableArguments.ShouldBe(new[] { new ReachableArgument(to2, to2.Owner, sequence1) });
        }
        [Fact]
        public void IfThenElseBranchWithVars()
        {
            var root = Load(TestXamls.IfThenElseBranchWithVars);
            var if1 = (If)root.Activities[1];
            var writeLine3 = if1.Then.Children.Last();
            var to5 = root.Activities[0].RuntimeArguments[1];
            var to6 = if1.Then.Children[0].RuntimeArguments[1];

            var locations = ScopeUtils.GetCompatibleLocations(writeLine3, IsCompatible, ArgumentsFilter);
            
            locations.Locals.ShouldBe(if1.Then.RuntimeVariables);
            locations.ReachableArguments.ShouldBe(new[] { new ReachableArgument(to6, to6.Owner, if1.Then), new ReachableArgument(to5, to5.Owner, root) });

            var writeLine4 = if1.Else.Children.Last();
            var to7 = if1.Else.Children[0].RuntimeArguments[1];

            locations = ScopeUtils.GetCompatibleLocations(writeLine4, IsCompatible, ArgumentsFilter);

            locations.Locals.ShouldBe(if1.Else.RuntimeVariables);
            locations.ReachableArguments.ShouldBe(new[] { new ReachableArgument(to7, to7.Owner, if1.Else), new ReachableArgument(to5, to5.Owner, root) });
        }
    }
    public class ScopeActivity : Activity
    {
        public ScopeActivity() => Body = new ActivityAction<int> { Handler = new Sequence(), Argument = new("actionArgument") };
        public ActivityAction<int> Body { get; set; }
    }
}