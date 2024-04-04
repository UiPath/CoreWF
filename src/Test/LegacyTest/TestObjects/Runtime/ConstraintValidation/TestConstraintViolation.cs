// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Validation;
using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.Runtime.ConstraintValidation
{
    public class TestConstraintViolation
    {
        private readonly List<string> _messageSubstrings;

        public TestConstraintViolation(string message = null, Activity source = null, bool isWarning = false, string propertyName = null)
        {
            this.Activity = source;
            this.Message = message;
            this.IsWarning = isWarning;
            this.PropertyName = propertyName;
            _messageSubstrings = new List<string>();
        }

        public TestConstraintViolation(string message, string source, string propertyName = null, object sourceDetail = null)
        {
            //This constructor is specifically designed to test the validation errors coming from expressions
            this.SourceName = source;
            this.Message = message;
            this.IsWarning = false;
            this.PropertyName = propertyName;
            this.SourceDetail = sourceDetail;
            _messageSubstrings = new List<string>();
        }

        public string SourceName
        {
            get;
            set;
        }

        public Activity Activity
        {
            get;
            set;
        }

        public bool IsWarning
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public string PropertyName
        {
            get;
            set;
        }

        public object SourceDetail
        {
            get;
            set;
        }

        public List<string> MessageSubstrings
        {
            get { return _messageSubstrings; }
        }

        public bool IsMatching(ValidationError actualError)
        {
            string sourceDisplayName = this.Activity == null ? string.Empty : this.Activity.DisplayName;

            if (this.SourceName != null)
            {
                sourceDisplayName = this.SourceName;
            }
            string errorSourceDisplayName = actualError.Source == null ? string.Empty : actualError.Source.DisplayName;

            if (sourceDisplayName != errorSourceDisplayName)
            {
                return false;
            }

            if ((this.Message != null) &&
                (this.Message != actualError.Message))
            {
                return false;
            }

            if ((this.PropertyName != null) &&
                (this.PropertyName != actualError.PropertyName))
            {
                return false;
            }

            if ((this.SourceDetail != null) &&
                (this.SourceDetail != actualError.SourceDetail))
            {
                return false;
            }

            if (this.MessageSubstrings.Count != 0)
            {
                foreach (string currentSubstring in this.MessageSubstrings)
                {
                    if (!actualError.Message.Contains(currentSubstring))
                    {
                        return false;
                    }
                }
            }

            if (this.IsWarning != actualError.IsWarning)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            //If actual constraint comes from other than activity then this.Activity will be null
            string activityDisplayName = this.Activity == null ? string.Empty : this.Activity.DisplayName;

            if (this.SourceName != null)
            {
                activityDisplayName = this.SourceName;
            }

            string message = this.Message;

            if (message == null && this.MessageSubstrings.Count != 0)
            {
                message = String.Join("; ", this.MessageSubstrings.ToArray());
            }

            //when requirements change and need to validate only an ErrorCode, it can be handled here
            string constraint = string.Format("Activity={0}, {1}: {2}", activityDisplayName, (this.IsWarning ? "Warning" : "Error"), message);
            return constraint;
        }

        public static string ActualConstraintViolationToString(ValidationError error)
        {
            //If actual constraint comes from other than activity then this.Activity will be null
            string activityDisplayName = error.Source == null ? string.Empty : error.Source.DisplayName;

            //when requirements change and need to validate only an ErrorCode, it can be handled here
            string constraint = string.Format("Activity={0}, {1}: {2}", activityDisplayName, (error.IsWarning ? "Warning" : "Error"), error.Message);
            return constraint;
        }
    }
}
