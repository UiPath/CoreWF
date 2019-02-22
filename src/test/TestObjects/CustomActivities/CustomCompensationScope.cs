// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;

namespace Test.Common.TestObjects.CustomActivities
{
    public class CustomCompensationScope : Activity
    {
        private Variable<CompensationToken> _handle = new Variable<CompensationToken>() { Name = "handle" };
        private readonly Variable<Exception> _exception = new Variable<Exception>() { Name = "exception" };

        public CustomCompensationScope()
        {
            base.Implementation = () => this.InternalActivities;
        }

        public Activity CSBody
        {
            get;
            set;
        }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            // None
        }

        private Sequence InternalActivities
        {
            get
            {
                return new Sequence
                {
                    DisplayName = "CustomCS_Sequence",
                    Variables = { _handle },
                    Activities =
                    {
                        new TryCatch
                        {
                            DisplayName = "CustomCS_TryCatch",
                            Try = new CompensableActivity
                            {
                                DisplayName = "CustomCS_CA",
                                Result = _handle,
                                Body = this.CSBody
                            },
                            Finally = new If((env) => _handle.Get(env) != null)
                            {
                                DisplayName = "CustomCS_If",
                                Then = new Confirm
                                {
                                    DisplayName = "CustomCS_Confirm",
                                    Target = _handle
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}
