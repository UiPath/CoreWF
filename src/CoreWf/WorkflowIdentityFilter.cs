// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace CoreWf
{
    public enum WorkflowIdentityFilter
    {
        Exact = 0,
        Any = 1,
        AnyRevision = 2
    }

    internal static class WorkflowIdentityFilterExtensions
    {
        public static bool IsValid(this WorkflowIdentityFilter value)
        {
            return (int)value >= (int)WorkflowIdentityFilter.Exact && (int)value <= (int)WorkflowIdentityFilter.AnyRevision;
        }
    }
}
