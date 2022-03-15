// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace System.Activities.Validation;

public static class ValidationHelper
{
    public static void ValidateArguments(
        Activity activity,
        OverloadGroupEquivalenceInfo equivalenceInfo,
        Dictionary<string, List<RuntimeArgument>> overloadGroups,
        List<RuntimeArgument> requiredArgumentsNotInOverloadGroups,
        IDictionary<string, object> inputs,
        ref IList<ValidationError> validationErrors)
    {
        if (!requiredArgumentsNotInOverloadGroups.IsNullOrEmpty())
        {
            // 1. Check if there are any Required arguments (outside overload groups) that were not specified.
            foreach (RuntimeArgument argument in requiredArgumentsNotInOverloadGroups)
            {
                if (CheckIfArgumentIsNotBound(argument, inputs))
                {
                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.RequiredArgumentValueNotSupplied(argument.Name), false, argument.Name, activity));
                }
            }
        }

        if (!overloadGroups.IsNullOrEmpty())
        {
            //1. Check to see if any of the overload groups are configured. 
            // An overload group is considered to be completely configured if all it's required arguments
            // are non-null. If an overload group does not have any required arguments then the group is 
            // considered configured if any of the optional arguments are configured.
            Dictionary<string, bool> configurationResults = new();
            string configuredGroupName = string.Empty;
            int configuredCount = 0;
            int overloadGroupsWithNoRequiredArgs = 0;

            foreach (KeyValuePair<string, List<RuntimeArgument>> entry in overloadGroups)
            {
                string groupName = entry.Key;
                configurationResults.Add(groupName, false);
                IEnumerable<RuntimeArgument> requiredArguments = entry.Value.Where((a) => a.IsRequired);

                if (requiredArguments.Any())
                {
                    if (requiredArguments.All(localArgument => CheckIfArgumentIsBound(localArgument, inputs)))
                    {
                        configurationResults[groupName] = true;
                        configuredGroupName = groupName;
                        configuredCount++;
                    }
                }
                else
                {
                    overloadGroupsWithNoRequiredArgs++;
                    IEnumerable<RuntimeArgument> optionalArguments = entry.Value.Where((a) => !a.IsRequired);
                    if (optionalArguments.Any(localArgument => CheckIfArgumentIsBound(localArgument, inputs)))
                    {
                        configurationResults[groupName] = true;
                        configuredGroupName = groupName;
                        configuredCount++;
                    }
                }
            }

            //2. It's an error if none of the groups are configured unless there
            // is atleast one overload group with no required arguments in it.
            if (configuredCount == 0)
            {
                if (overloadGroupsWithNoRequiredArgs == 0)
                {
                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.NoOverloadGroupsAreConfigured, false, activity));
                }
            }
            //3. If only one overload group was configured, ensure none of the disjoint/overlapping groups have any 
            // required or optional activity arguments set.
            else if (configuredCount == 1)
            {
                HashSet<RuntimeArgument> configuredOverloadSet = new(overloadGroups[configuredGroupName]);
                Predicate<RuntimeArgument> checkIfArgumentIsBound = new(localArgument => CheckIfArgumentIsBound(localArgument, inputs));

                List<string> disjointGroups = null;
                if (!equivalenceInfo.DisjointGroupsDictionary.IsNullOrEmpty())
                {
                    equivalenceInfo.DisjointGroupsDictionary.TryGetValue(configuredGroupName, out disjointGroups);
                }

                List<string> overlappingGroups = null;
                if (!equivalenceInfo.OverlappingGroupsDictionary.IsNullOrEmpty())
                {
                    equivalenceInfo.OverlappingGroupsDictionary.TryGetValue(configuredGroupName, out overlappingGroups);
                }

                // Iterate over the groups that may not be completely configured.
                foreach (string groupName in configurationResults.Keys.Where((k) => configurationResults[k] == false))
                {
                    // Check if the partially configured group name is in the disjoint groups list. 
                    // If so, find all configured arguments.
                    if (disjointGroups != null && disjointGroups.Contains(groupName))
                    {
                        foreach (RuntimeArgument configuredArgument in overloadGroups[groupName].FindAll(checkIfArgumentIsBound))
                        {
                            ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ExtraOverloadGroupPropertiesConfigured(configuredGroupName,
                                configuredArgument.Name, groupName), false, activity));
                        }
                    }
                    else if (overlappingGroups != null && overlappingGroups.Contains(groupName))
                    {
                        // Find all arguments of the Overlapping group that are not in the configuredOverloadSet.
                        HashSet<RuntimeArgument> overloadGroupSet = new(overloadGroups[groupName]);
                        IEnumerable<RuntimeArgument> intersectSet = overloadGroupSet.Intersect(configuredOverloadSet);
                        List<RuntimeArgument> exceptList = overloadGroupSet.Except(intersectSet).ToList();

                        foreach (RuntimeArgument configuredArgument in exceptList.FindAll(checkIfArgumentIsBound))
                        {
                            ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ExtraOverloadGroupPropertiesConfigured(configuredGroupName,
                                configuredArgument.Name, groupName), false, activity));
                        }
                    }
                }
            }
            //4. If more than one overload group is configured, generate an error.
            else
            {
                IEnumerable<string> configuredGroups = configurationResults.Keys.Where((k) => configurationResults[k]).OrderBy((k) => k, StringComparer.Ordinal);
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.MultipleOverloadGroupsConfigured(configuredGroups.AsCommaSeparatedValues()), false, activity));
            }
        }
    }

    public static bool GatherAndValidateOverloads(
        Activity activity,
        out Dictionary<string, List<RuntimeArgument>> overloadGroups,
        out List<RuntimeArgument> requiredArgumentsNotInOverloadGroups,
        out OverloadGroupEquivalenceInfo equivalenceInfo,
        ref IList<ValidationError> validationErrors)
    {
        overloadGroups = null;
        requiredArgumentsNotInOverloadGroups = null;
        IEnumerable<RuntimeArgument> runtimeArguments = activity.RuntimeArguments;

        foreach (RuntimeArgument runtimeArgument in runtimeArguments)
        {
            if (!runtimeArgument.OverloadGroupNames.IsNullOrEmpty())
            {
                foreach (string groupName in runtimeArgument.OverloadGroupNames)
                {
                    overloadGroups ??= new Dictionary<string, List<RuntimeArgument>>();
                    if (!overloadGroups.TryGetValue(groupName, out List<RuntimeArgument> arguments))
                    {
                        arguments = new List<RuntimeArgument>();
                        overloadGroups.Add(groupName, arguments);
                    }
                    arguments.Add(runtimeArgument);
                }
            }
            else
            {
                if (runtimeArgument.IsRequired)
                {
                    requiredArgumentsNotInOverloadGroups ??= new List<RuntimeArgument>();
                    requiredArgumentsNotInOverloadGroups.Add(runtimeArgument);
                }
            }
        }

        equivalenceInfo = GetOverloadGroupEquivalence(overloadGroups);

        return ValidateOverloadGroupDefinitions(activity, equivalenceInfo, overloadGroups, ref validationErrors);
    }


    // This method checks if any of the overload groups are equivalent and/or are a subset/superset of another
    // overload group.  Returns true if there are not any errors.
    private static bool ValidateOverloadGroupDefinitions(Activity activity, OverloadGroupEquivalenceInfo equivalenceInfo, Dictionary<string, List<RuntimeArgument>> overloadGroups, ref IList<ValidationError> validationErrors)
    {
        Fx.Assert(equivalenceInfo != null, "equivalenceInfo should have been setup before calling this method");

        bool noErrors = true;

        if (!equivalenceInfo.EquivalentGroupsDictionary.IsNullOrEmpty())
        {
            Hashtable keysVisited = new(equivalenceInfo.EquivalentGroupsDictionary.Count);
            foreach (KeyValuePair<string, List<string>> entry in equivalenceInfo.EquivalentGroupsDictionary)
            {
                if (!keysVisited.Contains(entry.Key))
                {
                    string[] equivalentGroups = new string[entry.Value.Count + 1];
                    equivalentGroups[0] = entry.Key;
                    entry.Value.CopyTo(equivalentGroups, 1);

                    IEnumerable<string> sortedList = equivalentGroups.OrderBy((s) => s, StringComparer.Ordinal);
                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.OverloadGroupsAreEquivalent(sortedList.AsCommaSeparatedValues()), false, activity));
                    noErrors = false;

                    for (int i = 0; i < equivalentGroups.Length; i++)
                    {
                        keysVisited.Add(equivalentGroups[i], null);
                    }
                }
            }
        }
        else if (!equivalenceInfo.SupersetOfGroupsDictionary.IsNullOrEmpty())
        {
            foreach (KeyValuePair<string, List<string>> entry in equivalenceInfo.SupersetOfGroupsDictionary)
            {
                IList<string> sortedList = entry.Value.OrderBy((s) => s, StringComparer.Ordinal).ToList();
                string[] subsetGroups = new string[sortedList.Count];
                int index = 0;

                // Select only subsets that have atleast one required argument in them.
                // We ignore the subsets that have no required arguments in them.
                foreach (string subsetGroup in sortedList)
                {
                    if (overloadGroups[subsetGroup].Any((a) => a.IsRequired))
                    {
                        subsetGroups[index++] = subsetGroup;
                    }
                }

                // If there were any subsets with required arguments generate an error.
                if (index > 0)
                {
                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.OverloadGroupHasSubsets(entry.Key, subsetGroups.AsCommaSeparatedValues()), false, activity));
                    noErrors = false;
                }
            }
        }

        return noErrors;
    }

    private static OverloadGroupEquivalenceInfo GetOverloadGroupEquivalence(Dictionary<string, List<RuntimeArgument>> groupDefinitions)
    {
        OverloadGroupEquivalenceInfo overloadGroupsInfo = new();

        if (!groupDefinitions.IsNullOrEmpty())
        {
            string[] groupNames = new string[groupDefinitions.Count];
            groupDefinitions.Keys.CopyTo(groupNames, 0);

            for (int i = 0; i < groupNames.Length; i++)
            {
                string group1 = groupNames[i];
                HashSet<RuntimeArgument> group1Args = new(groupDefinitions[group1]);
                for (int j = i + 1; j < groupNames.Length; j++)
                {
                    string group2 = groupNames[j];
                    HashSet<RuntimeArgument> group2Args = new(groupDefinitions[group2]);

                    if (group1Args.IsProperSupersetOf(group2Args))
                    {
                        overloadGroupsInfo.SetAsSuperset(group1, group2);
                    }
                    else if (group1Args.IsProperSubsetOf(group2Args))
                    {
                        overloadGroupsInfo.SetAsSuperset(group2, group1);
                    }
                    else if (group1Args.SetEquals(group2Args))
                    {
                        overloadGroupsInfo.SetAsEquivalent(group1, group2);
                    }
                    else if (group1Args.Overlaps(group2Args))
                    {
                        overloadGroupsInfo.SetAsOverlapping(group1, group2);
                    }
                    else // the groups are disjoint.
                    {
                        overloadGroupsInfo.SetAsDisjoint(group1, group2);
                    }
                }
            }
        }

        return overloadGroupsInfo;
    }

    private static bool CheckIfArgumentIsNotBound(RuntimeArgument argument, IDictionary<string, object> inputs)
    {
        if (argument.Owner != null && argument.Owner.Parent == null && ArgumentDirectionHelper.IsOut(argument.Direction))
        {
            // Skip the validation for root node's out argument
            // as it will be added to the output dictionary
            return false;
        }

        if (argument.BoundArgument != null && argument.BoundArgument.Expression != null)
        {
            return false;
        }
        if (inputs != null && inputs.ContainsKey(argument.Name))
        {
            return false;
        }
        return true;
    }

    private static bool CheckIfArgumentIsBound(RuntimeArgument argument, IDictionary<string, object> inputs) => !(CheckIfArgumentIsNotBound(argument, inputs));

    public class OverloadGroupEquivalenceInfo
    {
        private Dictionary<string, List<string>> _equivalentGroupsDictionary;
        private Dictionary<string, List<string>> _supersetOfGroupsDictionary;
        private Dictionary<string, List<string>> _overlappingGroupsDictionary;
        private Dictionary<string, List<string>> _disjointGroupsDictionary;

        public OverloadGroupEquivalenceInfo() { }

        public Dictionary<string, List<string>> EquivalentGroupsDictionary => _equivalentGroupsDictionary;

        public Dictionary<string, List<string>> SupersetOfGroupsDictionary => _supersetOfGroupsDictionary;

        public Dictionary<string, List<string>> OverlappingGroupsDictionary => _overlappingGroupsDictionary;

        public Dictionary<string, List<string>> DisjointGroupsDictionary => _disjointGroupsDictionary;

        public void SetAsEquivalent(string group1, string group2)
        {
            // Setup EquivalentGroups for group1
            AddToDictionary(ref _equivalentGroupsDictionary, group1, group2);

            // Setup EquivalentGroups for group2
            AddToDictionary(ref _equivalentGroupsDictionary, group2, group1);
        }

        public void SetAsSuperset(string group1, string group2) => AddToDictionary(ref _supersetOfGroupsDictionary, group1, group2);

        public void SetAsOverlapping(string group1, string group2)
        {
            // Setup OverlapGroups for group1
            AddToDictionary(ref _overlappingGroupsDictionary, group1, group2);

            // Setup OverlapGroups for group2
            AddToDictionary(ref _overlappingGroupsDictionary, group2, group1);
        }

        public void SetAsDisjoint(string group1, string group2)
        {
            // Setup DisjointGroups for group1
            AddToDictionary(ref _disjointGroupsDictionary, group1, group2);

            // Setup DisjointGroups for group2
            AddToDictionary(ref _disjointGroupsDictionary, group2, group1);
        }

        private static void AddToDictionary(ref Dictionary<string, List<string>> dictionary, string dictionaryKey, string listEntry)
        {
            dictionary ??= new Dictionary<string, List<string>>();

            if (!dictionary.TryGetValue(dictionaryKey, out List<string> listValues))
            {
                listValues = new List<string> { listEntry };
                dictionary.Add(dictionaryKey, listValues);
            }
            else
            {
                Fx.Assert(!listValues.Contains(listEntry), string.Format(CultureInfo.InvariantCulture, "Duplicate group entry '{0}' getting added.", listEntry));
                listValues.Add(listEntry);
            }
        }
    }
}
