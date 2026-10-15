#nullable enable
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TestCases.Workflows;

public class LocationAccessExpressionCacheTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CreateLocationAccessExpression_RepeatedType_ReturnsFreshActivitiesWithTheirOwnLocation(bool isReference, bool legacy)
    {
        var firstLocation = new Variable<int>("first");
        var secondLocation = new Variable<int>("second");

        var first = ActivityUtilities.CreateLocationAccessExpression(firstLocation, isReference, legacy);
        var second = ActivityUtilities.CreateLocationAccessExpression(secondLocation, isReference, legacy);

        first.ShouldNotBeSameAs(second);
        first.GetType().ShouldBe(ExpectedType(typeof(int), isReference, legacy));
        second.GetType().ShouldBe(first.GetType());
        ((ILocationReferenceExpression)first).LocationReference.ShouldBeSameAs(firstLocation);
        ((ILocationReferenceExpression)second).LocationReference.ShouldBeSameAs(secondLocation);
        first.Parent.ShouldBeNull();
        second.Parent.ShouldBeNull();
        first.IsMetadataCached.ShouldBeFalse();
        second.IsMetadataCached.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateLocationAccessExpression_ConcurrentFirstUse_PreservesEveryLocationAndActivityIdentity(bool isReference, bool legacy)
    {
        var references = Enumerable.Range(0, 128).Select(index => new Variable<ColdType>($"value{index}")).ToArray();
        var expressions = new ActivityWithResult[references.Length];

        Parallel.For(0, references.Length, index =>
        {
            expressions[index] = ActivityUtilities.CreateLocationAccessExpression(references[index], isReference, legacy);
        });

        expressions.Distinct().Count().ShouldBe(references.Length);
        for (int index = 0; index < references.Length; index++)
        {
            expressions[index].GetType().ShouldBe(ExpectedType(typeof(ColdType), isReference, legacy));
            ((ILocationReferenceExpression)expressions[index]).LocationReference.ShouldBeSameAs(references[index]);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateLocationAccessExpression_InvalidType_RetriesWithoutCachingTheFailure(bool isReference, bool legacy)
    {
        var location = new InvalidLocation();

        var first = Should.Throw<ArgumentException>(() => ActivityUtilities.CreateLocationAccessExpression(location, isReference, legacy));
        var second = Should.Throw<ArgumentException>(() => ActivityUtilities.CreateLocationAccessExpression(location, isReference, legacy));

        second.ShouldNotBeSameAs(first);
        second.Message.ShouldBe(first.Message);
        second.ParamName.ShouldBe(first.ParamName);
        location.TypeReads.ShouldBe(2);
    }

    private static Type ExpectedType(Type valueType, bool isReference, bool legacy) =>
        (legacy ? typeof(LocationReferenceValue<>) : isReference ? typeof(EnvironmentLocationReference<>) : typeof(EnvironmentLocationValue<>))
            .MakeGenericType(valueType);

    private sealed class ColdType { }

    private sealed class InvalidLocation : LocationReference
    {
        public int TypeReads { get; private set; }
        protected override string NameCore => "invalid";
        protected override Type TypeCore { get { TypeReads++; return typeof(void); } }
        public override Location GetLocation(ActivityContext context) => throw new NotSupportedException();
    }
}
