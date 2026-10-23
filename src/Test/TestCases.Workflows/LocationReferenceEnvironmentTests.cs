#nullable enable
using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace TestCases.Workflows;

public class LocationReferenceEnvironmentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    public void Declare_AnyScopeSize_PreservesLookupVisibilityAndDeclarationOrder(int count)
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var locations = Enumerable.Range(0, count).Select(index => new Variable<int>($"value{index}")).ToArray();
        foreach (var location in locations)
        {
            Declare(environment, location).ShouldBeEmpty();
            environment.TryGetLocationReference(location.Name, out var actual).ShouldBeTrue();
            actual.ShouldBeSameAs(location);
            environment.IsVisible(location).ShouldBeTrue();
        }

        environment.TryGetLocationReference("missing", out var missing).ShouldBeFalse();
        missing.ShouldBeNull();
        environment.IsVisible(new Variable<int>("value0")).ShouldBeFalse();
        environment.GetLocationReferences().ShouldBe(locations);
        environment.GetLocationReferences().ShouldBe(locations);
    }

    [Fact]
    public void Declare_DuplicateBeforeAndAfterPromotion_PreservesDiagnosticsAndOriginalReference()
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var owner = new Sequence();
        var original = new Variable<int>("same");
        Declare(environment, original).ShouldBeEmpty();

        var before = Declare(environment, new Variable<string>("same"), owner).Single();
        Declare(environment, new Variable<int>("other")).ShouldBeEmpty();
        var after = Declare(environment, new Variable<string>("same"), owner).Single();

        before.Message.ShouldBe(after.Message);
        before.Source.ShouldBeSameAs(owner);
        after.Source.ShouldBeSameAs(owner);
        before.Id.ShouldBe(owner.Id);
        after.Id.ShouldBe(owner.Id);
        before.IsWarning.ShouldBeFalse();
        after.IsWarning.ShouldBeFalse();
        environment.TryGetLocationReference("same", out var actual).ShouldBeTrue();
        actual.ShouldBeSameAs(original);
        environment.GetLocationReferences().Count().ShouldBe(2);
    }

    [Fact]
    public void Declare_ReferenceNameChanges_RetainsOriginalLookupKeyThroughPromotion()
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var location = new Variable<int>("original");
        Declare(environment, location).ShouldBeEmpty();
        location.Name = "renamed";

        environment.TryGetLocationReference("original", out var before).ShouldBeTrue();
        before.ShouldBeSameAs(location);
        environment.TryGetLocationReference("renamed", out _).ShouldBeFalse();
        Declare(environment, new Variable<int>("original")).Count.ShouldBe(1);
        Declare(environment, new Variable<int>("other")).ShouldBeEmpty();
        environment.TryGetLocationReference("original", out var after).ShouldBeTrue();
        after.ShouldBeSameAs(location);
        environment.GetLocationReferences().First().ShouldBeSameAs(location);
    }

    [Fact]
    public void Declare_CaseDistinctNames_RemainDistinctInSmallAndPromotedScopes()
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var upper = new Variable<int>("Value");
        var lower = new Variable<int>("value");
        Declare(environment, upper).ShouldBeEmpty();
        environment.TryGetLocationReference("value", out _).ShouldBeFalse();
        Declare(environment, lower).ShouldBeEmpty();

        environment.TryGetLocationReference("Value", out var first).ShouldBeTrue();
        environment.TryGetLocationReference("value", out var second).ShouldBeTrue();
        first.ShouldBeSameAs(upper);
        second.ShouldBeSameAs(lower);
        environment.GetLocationReferences().ShouldBe(new[] { upper, lower });
    }

    [Fact]
    public void GetLocationReferences_RetainedEmptyView_ObservesLaterDeclarations()
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var view = environment.GetLocationReferences();
        var first = new Variable<int>("first");
        var second = new Variable<int>("second");
        view.ShouldBeEmpty();

        Declare(environment, first).ShouldBeEmpty();
        view.ShouldBe(new[] { first });
        Declare(environment, second).ShouldBeEmpty();

        view.ShouldBe(new[] { first, second });
        environment.GetLocationReferences().ShouldBeSameAs(view);
    }

    [Fact]
    public void GetLocationReferences_RetainedSingletonView_ObservesPromotionAndInvalidatesActiveEnumerator()
    {
        var environment = new ActivityLocationReferenceEnvironment();
        var first = new Variable<int>("first");
        var second = new Variable<int>("second");
        Declare(environment, first).ShouldBeEmpty();
        var view = environment.GetLocationReferences();
        using var enumerator = view.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        Declare(environment, second).ShouldBeEmpty();

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
        view.ShouldBe(new[] { first, second });
        environment.GetLocationReferences().ShouldBeSameAs(view);
    }

    [Fact]
    public void Declare_ParentShadowing_PreservesIdentityVisibilityAndHostFallback()
    {
        var hostValue = new Variable<int>("host");
        var host = new HostEnvironment(hostValue);
        var parent = new ActivityLocationReferenceEnvironment(host);
        var child = new ActivityLocationReferenceEnvironment(new ActivityLocationReferenceEnvironment(parent));
        var parentValue = new Variable<int>("value");
        var childValue = new Variable<int>("value");
        Declare(parent, parentValue).ShouldBeEmpty();
        Declare(child, childValue).ShouldBeEmpty();

        child.TryGetLocationReference("value", out var actual).ShouldBeTrue();
        actual.ShouldBeSameAs(childValue);
        child.IsVisible(parentValue).ShouldBeTrue();
        child.IsVisible(childValue).ShouldBeTrue();
        child.IsVisible(hostValue).ShouldBeTrue();
        child.TryGetLocationReference("host", out var hostActual).ShouldBeTrue();
        hostActual.ShouldBeSameAs(hostValue);
        child.GetLocationReferences().ShouldBe(new[] { childValue });
        parent.TryGetLocationReference("value", out var parentActual).ShouldBeTrue();
        parentActual.ShouldBeSameAs(parentValue);
    }

    [Fact]
    public void Declare_UnnamedReferences_PreservesVisibilityWithoutNamedEnumerationOrLookup()
    {
        var hostValue = new Variable<int>();
        var environment = new ActivityLocationReferenceEnvironment(new HostEnvironment(hostValue));
        var localValue = new Variable<int>();
        Declare(environment, localValue).ShouldBeEmpty();

        environment.IsVisible(localValue).ShouldBeTrue();
        environment.GetLocationReferences().ShouldBeEmpty();
        environment.TryGetLocationReference(null, out var actual).ShouldBeTrue();
        actual.ShouldBeSameAs(hostValue);
        new ActivityLocationReferenceEnvironment().TryGetLocationReference(null, out var missing).ShouldBeFalse();
        missing.ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => environment.IsVisible(null));
    }

    private static IList<ValidationError> Declare(ActivityLocationReferenceEnvironment environment, LocationReference location, Activity? owner = null)
    {
        IList<ValidationError> errors = new List<ValidationError>();
        environment.Declare(location, owner, ref errors);
        return errors;
    }

    private sealed class HostEnvironment(LocationReference location) : LocationReferenceEnvironment
    {
        public override Activity? Root => null;
        public override IEnumerable<LocationReference> GetLocationReferences() => new[] { location };
        public override bool IsVisible(LocationReference reference) => ReferenceEquals(reference, location);
        public override bool TryGetLocationReference(string name, out LocationReference result)
        {
            result = location;
            return string.Equals(name, location.Name, StringComparison.Ordinal);
        }
    }
}
