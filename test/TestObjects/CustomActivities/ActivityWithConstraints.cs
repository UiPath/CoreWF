// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using CoreWf.Validation;

namespace Test.Common.TestObjects.CustomActivities
{
    public class ActivityWithConstraints : NativeActivity
    {
        public ActivityWithConstraints()
        {
            base.Constraints.Add(MustHaveBodyConstraint());
        }

        public Activity Body
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // None
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (this.Body != null)
            {
                context.ScheduleActivity(this.Body);
            }
        }

        public static ValidationSettings CreateValidatorSettings()
        {
            return new ValidationSettings
            {
                AdditionalConstraints =
                {
                    { typeof(Sequence), new List<Constraint> { MustHaveChild() } },
                    { typeof(Activity), new List<Constraint> { DisplayNameMustStartWithLetter() } }
                }
            };
        }

        private static Constraint<ActivityWithConstraints> MustHaveBodyConstraint()
        {
            DelegateInArgument<ActivityWithConstraints> scope = new DelegateInArgument<ActivityWithConstraints>();

            return new Constraint<ActivityWithConstraints>
            {
                Body = new ActivityAction<ActivityWithConstraints, ValidationContext>
                {
                    Argument1 = scope,
                    Handler = new AssertValidation
                    {
                        IsWarning = true,
                        Assertion = new InArgument<bool>((env) => scope.Get(env).Body != null),
                        Message = new InArgument<string>((env) => string.Format("Activity '{0}' should have a Body.", scope.Get(env).DisplayName))
                    }
                }
            };
        }

        private static Constraint<Sequence> MustHaveChild()
        {
            DelegateInArgument<Sequence> sequence = new DelegateInArgument<Sequence>();

            return new Constraint<Sequence>
            {
                Body = new ActivityAction<Sequence, ValidationContext>
                {
                    Argument1 = sequence,
                    Handler = new AssertValidation
                    {
                        IsWarning = true,
                        Assertion = new InArgument<bool>((env) => sequence.Get(env).Activities.Count > 0),
                        Message = new InArgument<string>((env) => string.Format("Sequence '{0}' should have at least 1 child.", sequence.Get(env).DisplayName))
                    }
                }
            };
        }

        private static Constraint<Activity> DisplayNameMustStartWithLetter()
        {
            DelegateInArgument<Activity> element = new DelegateInArgument<Activity>();

            return new Constraint<Activity>
            {
                Body = new ActivityAction<Activity, ValidationContext>
                {
                    Argument1 = element,
                    Handler = new AssertValidation
                    {
                        IsWarning = false,
                        Assertion = new InArgument<bool>((env) => char.IsLetter(element.Get(env).DisplayName[0])),
                        Message = new InArgument<string>((env) => string.Format("Display name for '{0}' must start with a letter.", element.Get(env).DisplayName))
                    }
                }
            };
        }
    }
}
