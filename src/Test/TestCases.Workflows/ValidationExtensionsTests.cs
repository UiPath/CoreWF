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
            seq.Activities.Add(new ActivityWithValidationExtension());
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
                new ActivityWithValidationExtension(),
                new ActivityWithValidationError(),
                new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("var1")) }
            }
        };

        var result = ActivityValidationServices.Validate(seq, _useValidator);
        result.Errors.Count.ShouldBe(3);
        result.Errors.ShouldContain(error => error.Message.Contains(nameof(ActivityWithValidationError)));
        result.Errors.ShouldContain(error => error.Message.Contains(nameof(MockValidationExtension)));
        result.Errors.ShouldContain(error => error.Message.Contains("The name 'var1' does not exist in the current context"));
    }

    class ActivityWithValidationError : CodeActivity
    {
        protected override void Execute(CodeActivityContext context) => throw new NotImplementedException();

        protected override void CacheMetadata(CodeActivityMetadata metadata) => metadata.AddValidationError(nameof(ActivityWithValidationError));
    }

    class ActivityWithValidationExtension : CodeActivity
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
        public IEnumerable<ValidationError> PostValidate(Activity activity, ValidationSettings validationSettings) =>
            new List<ValidationError>() { new ValidationError(nameof(MockValidationExtension)) };
    }
}