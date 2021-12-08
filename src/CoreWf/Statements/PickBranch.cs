// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Action")]
public sealed class PickBranch
{
    private Collection<Variable> _variables;
    private string _displayName;

    public PickBranch()
    {
        _displayName = "PickBranch";
    }

    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }

    [DefaultValue(null)]
    [DependsOn("Variables")]
    public Activity Trigger { get; set; }

    [DefaultValue(null)]
    [DependsOn("Trigger")]
    public Activity Action { get; set; }

    // TODO, 41221, remove this once we have well known attached properties.
    [DefaultValue("PickBranch")]
    public string DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }
}
