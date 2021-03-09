using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;

namespace System.Activities
{
    public record Locations(IReadOnlyCollection<LocationReference> Locals, IReadOnlyCollection<ReachableArgument> ReachableArguments)
    {
    }
    public record ReachableArgument(LocationReference Location, Activity Owner, Activity ScopeOwner)
    {
    }
    public static class ScopeUtils
    {
        public static Locations GetCompatibleLocations(this Activity anchor, Type type)
        {
            var locals = new List<LocationReference>();
            var environment = anchor.PublicEnvironment;
            var reachableArguments = new List<ReachableArgument>();
            Activity current;
            var currentChild = anchor;
            while (environment != null)
            {
                locals.AddRange(environment.GetLocationReferences().AssignableTo(type));
                environment = environment.Parent;
                current = currentChild.Parent;
                if (current is Sequence sequence)
                {
                    reachableArguments.AddRange(sequence.Activities.TakeWhile(child => child != currentChild).SelectMany(child =>
                            child.RuntimeArguments.Where(arg => arg.Direction != ArgumentDirection.In && arg.BoundArgument.Expression == null).AssignableTo(type)
                            .Select(arg => new ReachableArgument(arg, child, sequence))));
                }
                currentChild = current;
            }
            return new Locations(locals, reachableArguments);
        }
        static IEnumerable<LocationReference> AssignableTo(this IEnumerable<LocationReference> locations, Type type) => locations.Where(l => type.IsAssignableFrom(l.Type));
    }
}