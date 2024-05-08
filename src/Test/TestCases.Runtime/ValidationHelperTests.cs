using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.ParallelTracking;
using System.Collections.Generic;
using System.Linq;
using WorkflowApplicationTestExtensions;
using Xunit;
using System.Activities.Validation;
using static System.Activities.Validation.ValidationHelper;
using System.ComponentModel;

namespace TestCases.Runtime;

public class ValidationHelperTests
{
    [Fact]
    public void ValidateArguments_UsesDisplayNameOfArgument()
    {
        var activity = new Sequence();
        var property = TypeDescriptor.GetProperties(activity)[0];
        var arguments = new List<RuntimeArgument>
        {
            new RuntimeArgument("argName", typeof(string), ArgumentDirection.In, true, new(), property, activity)
        };

        IList<ValidationError> errors = null;
        ValidateArguments(activity, new OverloadGroupEquivalenceInfo(), new Dictionary<string, List<RuntimeArgument>>(), arguments, new Dictionary<string, object>(), ref errors);

        errors.Count.ShouldBe(1);
        var error = errors.First();

        error.PropertyName.ShouldBe(arguments.First().Name);
        error.Message.Contains(property.DisplayName).ShouldBeTrue();
        error.Message.Contains(arguments.First().Name).ShouldBeFalse();
    }

    [Fact]
    public void ValidateArguments_DisplayNameNotAvailable()
    {
        var activity = new Sequence();
        var arguments = new List<RuntimeArgument>
        {
            new RuntimeArgument("argName", typeof(string), ArgumentDirection.In)
        };

        IList<ValidationError> errors = null;
        ValidateArguments(activity, new OverloadGroupEquivalenceInfo(), new Dictionary<string, List<RuntimeArgument>>(), arguments, new Dictionary<string, object>(), ref errors);

        errors.Count.ShouldBe(1);
        var error = errors.First();

        error.PropertyName.ShouldBe(arguments.First().Name);
        error.Message.Contains(arguments.First().Name).ShouldBeTrue();
    }
}

