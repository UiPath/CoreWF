// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Validation;

[Fx.Tag.XamlVisible(false)]
public sealed class ValidationContext
{
    private ActivityUtilities.ChildActivity _owner;
    private readonly ActivityUtilities.ActivityCallStack _parentChain;
    private readonly LocationReferenceEnvironment _environment;
    private IList<ValidationError> _getChildrenErrors;
    private readonly ProcessActivityTreeOptions _options;

    internal ValidationContext(ActivityUtilities.ChildActivity owner, ActivityUtilities.ActivityCallStack parentChain, ProcessActivityTreeOptions options, LocationReferenceEnvironment environment)
    {
        _owner = owner;
        _parentChain = parentChain;
        _options = options;
        _environment = environment;
    }

    internal LocationReferenceEnvironment Environment => _environment;

    internal IEnumerable<Activity> GetParents()
    {
        List<Activity> parentsList = new(_parentChain.Count);

        for (int i = 0; i < _parentChain.Count; i++)
        {
            parentsList.Add(_parentChain[i].Activity);
        }

        return parentsList;
    }

    internal IEnumerable<Activity> GetWorkflowTree()
    {
        // It is okay to just walk the declared parent chain here
        Activity currentNode = _owner.Activity;
        if (currentNode != null)
        {
            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
            }
            List<Activity> nodes = ActivityValidationServices.GetChildren(new ActivityUtilities.ChildActivity(currentNode, true), new ActivityUtilities.ActivityCallStack(), _options);
            nodes.Add(currentNode);
            return nodes;
        }
        else
        {
            return ActivityValidationServices.EmptyChildren;
        }
    }

    internal IEnumerable<Activity> GetChildren()
    {
        if (!_owner.Equals(ActivityUtilities.ChildActivity.Empty))
        {
            return ActivityValidationServices.GetChildren(_owner, _parentChain, _options);
        }
        else
        {
            return ActivityValidationServices.EmptyChildren;
        }
    }

    internal void AddGetChildrenErrors(ref IList<ValidationError> validationErrors)
    {
        if (_getChildrenErrors != null && _getChildrenErrors.Count > 0)
        {
            validationErrors ??= new List<ValidationError>();

            for (int i = 0; i < _getChildrenErrors.Count; i++)
            {
                validationErrors.Add(_getChildrenErrors[i]);
            }

            _getChildrenErrors = null;
        }
    }
}
