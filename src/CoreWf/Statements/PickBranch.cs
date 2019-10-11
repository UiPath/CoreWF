// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Activities.Internals;
    using System.Activities.Runtime.Collections;
    using System.Windows.Markup;

    [ContentProperty("Action")]
    public sealed class PickBranch
    {
        private Collection<Variable> variables;
        private string displayName;

        public PickBranch()
        {
            displayName = "PickBranch";
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (this.variables == null)
                {
                    this.variables = new ValidatingCollection<Variable>
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
                }
                return this.variables;
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
            get
            {
                return this.displayName;
            }
            set
            {
                this.displayName = value;
            }
        }
    }
}
