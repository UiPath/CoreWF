// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Action")]
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
                if (_variables == null)
                {
                    _variables = new ValidatingCollection<Variable>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _variables;
            }
        }

        [DefaultValue(null)]
        //[DependsOn("Variables")]
        public Activity Trigger { get; set; }

        [DefaultValue(null)]
        //[DependsOn("Trigger")]
        public Activity Action { get; set; }

        [DefaultValue("PickBranch")]
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
    }
}
