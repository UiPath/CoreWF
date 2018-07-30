// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoreWf;
using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public class TestWriteLine : TestActivity
    {
        private TestActivity _messageExpressionActivity;

        public TestWriteLine()
        {
            this.ProductActivity = new WriteLine();
        }

        public TestWriteLine(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestWriteLine(string displayName, string message)
            : this()
        {
            this.Message = message;
            this.DisplayName = displayName;
        }

        public TestWriteLine(string displayName, Variable<string> messageVariable, string hint)
            : this()
        {
            this.MessageVariable = messageVariable;
            this.DisplayName = displayName;
            this.HintMessage = hint;
        }

        public TestWriteLine(string displayName, Expression<Func<ActivityContext, String>> messageExpression, string hint)
            : this()
        {
            this.MessageExpression = messageExpression;
            this.DisplayName = displayName;
            this.HintMessage = hint;
        }

        public Variable<string> MessageVariable
        {
            set { this.ProductWriteLine.Message = new InArgument<string>(value); }
        }

        public Expression<Func<ActivityContext, String>> MessageExpression
        {
            set { this.ProductWriteLine.Message = new InArgument<string>(value); }
        }

        public TestActivity MessageActivity
        {
            set
            {
                this.ProductWriteLine.Message = (Activity<string>)(value.ProductActivity);
                _messageExpressionActivity = value;
            }
        }

        public string Message
        {
            set
            {
                this.ProductWriteLine.Message = new InArgument<string>(value);
                this.HintMessage = value;
            }
        }

        private List<string> _hintMessageList = null;
        public string HintMessage
        {
            set
            {
                this.HintMessageList.Add(value);
            }
        }

        public List<string> HintMessageList
        {
            get
            {
                if (_hintMessageList == null)
                {
                    _hintMessageList = new List<string>();
                }
                return _hintMessageList;
            }
        }

        public WriteLine ProductWriteLine
        {
            get { return (WriteLine)this.ProductActivity; }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (ExpectedOutcome.DefaultPropogationState != OutcomeState.Completed)
            {
                return;
            }

            if (_messageExpressionActivity != null)
            {
                _messageExpressionActivity.GetTrace(traceGroup);
            }

            if (this.HintMessageList.Count > 0
                && (this.HintMessageList.Count == 1
                || this.iterationNumber < this.HintMessageList.Count))
            {
                traceGroup.Steps.Add(new UserTrace(_hintMessageList[this.HintMessageList.Count == 1 ? 0 : this.iterationNumber]));
            }
            else
            {
                throw new ArgumentException("HintMessage or HintMessageList must be set");
            }
        }
    }
}
