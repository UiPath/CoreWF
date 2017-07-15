// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Reflection;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    /// <summary>
    /// This empty test activity can be used to add product tracing for activities which are hidden.
    /// </summary>
    internal class TestDummyTraceActivity : TestSequence
    {
        public TestDummyTraceActivity(String DisplayName)
        {
            this.ProductActivity = new Sequence();
            this.DisplayName = DisplayName;
        }

        public TestDummyTraceActivity(Activity element, Outcome outcome)
            : this(element.DisplayName)
        {
            // LambdaValue expression gets evaluated as VisualBasicValue, so we have to change name
            //  Need to make sure we dont overwrite the number of arguments (I.E.  LambdaValue'2) 
            //We do not use VisualBasicValue, so need not to replace
            //if (element.DisplayName.StartsWith("LambdaValue"))
            //{
            //    this.DisplayName = this.DisplayName.Replace("LambdaValue", "VisualBasicValue");
            //}
            this.ExpectedOutcome = outcome;
        }

        public TestDummyTraceActivity(Type type, Outcome outcome)
            : this(GetName(type))
        {
            this.ExpectedOutcome = outcome;
        }

        private static string GetName(Type t)
        {
            if (t.IsConstructedGenericType)
            {
                String name = t.Name.Substring(0, t.Name.IndexOf("`")) + "<";

                bool first = true;
                foreach (Type paramType in t.GetGenericArguments())
                {
                    if (!first)
                    {
                        name += ",";
                    }
                    name += paramType.Name;
                }
                return name + ">";
            }
            else
            {
                return t.Name;
            }
        }
    }
}
