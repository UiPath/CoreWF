using Microsoft.CSharp.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestCases.Workflows;

public class ValidationExtensionsTests
{
    private readonly ValidationSettings _useValidator = new() { ForceExpressionCache = false };

    [Fact]
    public void OnlyOneInstanceOfExtensionTypeIsAdded()
    {
        var seq = new Sequence();
        for (var j = 0; j < 10000; j++)
        {
            seq.Activities.Add(new ActivityWithValidationError());
        }

        var result = ActivityValidationServices.Validate(seq, _useValidator);
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain(nameof(MockValidationExtension));
    }

    [Fact]
    public void ValidationErrorsAreConcatenated()
    {
        var seq = new Sequence()
        {
            Activities =
            {
                new ActivityWithValidationError(),
                new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("var1")) }
            }
        };

        var result = ActivityValidationServices.Validate(seq, _useValidator);
        result.Errors.Count.ShouldBe(2);
        result.Errors.First().Message.ShouldContain("The name 'var1' does not exist in the current context");
        result.Errors.Last().Message.ShouldContain(nameof(MockValidationExtension));
    }

    class ActivityWithValidationError : CodeActivity
    {
        protected override void Execute(CodeActivityContext context) => throw new NotImplementedException();

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (metadata.Environment.IsValidating)
            {
                metadata.Environment.Extensions.GetOrAdd(() => new MockValidationExtension());
            }
        }
    }

    class MockValidationExtension : IValidationExtension
    {
        public IEnumerable<ValidationError> PostValidate(Activity activity) =>
            new List<ValidationError>() { new ValidationError(nameof(MockValidationExtension)) };
    }
}