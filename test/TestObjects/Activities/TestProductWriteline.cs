// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq.Expressions;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestProductWriteline : TestActivity
    {
        private ExpressionType _textExpressionType = ExpressionType.Literal;
        private TestActivity _testExpressionActivity;
        public TestProductWriteline()
        {
            this.ProductActivity = new WriteLine();
        }

        public TestProductWriteline(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public string Text
        {
            set { this.ProductWriteLine.Text = new InArgument<string>(value); }
        }

        public Variable<string> TextVariable
        {
            set { this.ProductWriteLine.Text = new InArgument<string>(value); }
        }

        public Expression<Func<ActivityContext, string>> TextExpression
        {
            set { this.ProductWriteLine.Text = new InArgument<string>(value); }
        }

        public Variable<TextWriter> TextWriterVariable
        {
            set { this.ProductWriteLine.TextWriter = new InArgument<TextWriter>(value); }
        }

        public Expression<Func<ActivityContext, TextWriter>> TextWriterExpression
        {
            set { this.ProductWriteLine.TextWriter = new InArgument<TextWriter>(value); }
        }

        public TestActivity TextExpressionActivity
        {
            set
            {
                _testExpressionActivity = value;
                ProductWriteLine.Text = (Activity<string>)value.ProductActivity;
                _textExpressionType = ExpressionType.Activity;
            }
        }

        public WriteLine ProductWriteLine
        {
            get
            {
                return (WriteLine)this.ProductActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (_textExpressionType == ExpressionType.Activity)
            {
                OrderedTraces orderedTraceGroup = new OrderedTraces();
                CurrentOutcome = _testExpressionActivity.GetTrace(orderedTraceGroup);
                traceGroup.Steps.Add(orderedTraceGroup);
            }
        }
    }
}
