// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Statements;
using System.Collections.ObjectModel;
using Xunit;

namespace Samples
{
    public class FlowChart : IDisposable
    {
        [Theory]
        [InlineData(true, false, false, "STD")]
        [InlineData(false, true, false, "MNC")]
        [InlineData(false, true, true, "MWC")]
        [InlineData(false, false, false, "NONE")]
        public void RunTest(bool student, bool married, bool kids, string discountCode)
        {
            var output = WorkflowInvoker.Invoke(new AutoDiscountFlowChart(student, married, kids));
            Assert.Equal(discountCode, output["DiscountCode"]);
        }

        public void Dispose()
        {
        }
    }

    public sealed class AutoDiscountFlowChart : Activity
    {
        private bool _isStudent;
        private bool _isMarried;
        private bool _haveChild;

        public AutoDiscountFlowChart(bool flgStudent, bool flgMarried, bool haveKids)
        {
            _isStudent = flgStudent;
            _isMarried = flgMarried;
            _haveChild = haveKids;
        }

        private OutArgument<string> DiscountCode { get; set; }

        private Activity GetImplementation()
        {
            Variable<double> discount = new Variable<double>();
            Variable<string> discountCode = new Variable<string>();
            Variable<bool> isStudent = new Variable<bool>() { Default = _isStudent };
            Variable<bool> isMarried = new Variable<bool> { Default = _isMarried };
            Variable<bool> haveChild = new Variable<bool> { Default = _haveChild };

            FlowStep returnDiscountCode = new FlowStep
            {
                Action = new Assign<string>
                {
                    DisplayName = "Return Discount Code",
                    To = new OutArgument<string>((ctx) => DiscountCode.Get(ctx)),
                    Value = new InArgument<string>(discountCode)
                },
                Next = null
            };

            FlowStep printDiscount = new FlowStep
            {
                Action = new WriteLine
                {
                    DisplayName = "WriteLine: Discount Applied",
                    Text = discountCode
                },
                Next = returnDiscountCode
            };

            FlowStep applyDefaultDiscount = new FlowStep
            {
                Action = new Assign<double>
                {
                    DisplayName = "Default Discount is 0%",
                    To = discount,
                    Value = new InArgument<double>(0)
                },
                Next = printDiscount
            };

            FlowStep applyBaseDiscount = new FlowStep
            {
                Action = new Assign<double>
                {
                    DisplayName = "Base Discount is 5%",
                    To = discount,
                    Value = new InArgument<double>(5)
                },
                Next = printDiscount
            };

            FlowStep applyStandardDiscount = new FlowStep
            {
                Action = new Assign<double>
                {
                    DisplayName = "Standard Discount is 10%",
                    To = discount,
                    Value = new InArgument<double>(10)
                },
                Next = printDiscount
            };

            FlowStep applyPremiumDiscount = new FlowStep
            {
                Action = new Assign<double>
                {
                    DisplayName = "Standard Discount is 15%",
                    To = discount,
                    Value = new InArgument<double>(15)
                },
                Next = printDiscount
            };

            FlowSwitch<string> discountCodeSwitch = new FlowSwitch<string>
            {
                Expression = discountCode,
                Cases =
                {
                    { "STD", applyBaseDiscount },
                    { "MNC", applyStandardDiscount },
                    { "MWC", applyPremiumDiscount},
                },
                Default = applyDefaultDiscount
            };

            FlowStep noDiscount = new FlowStep
            {
                Action = new Assign<string>
                {
                    DisplayName = "No Discount",
                    To = discountCode,
                    Value = new InArgument<string>("NONE")
                },
                Next = discountCodeSwitch
            };

            FlowStep studentDiscount = new FlowStep
            {
                Action = new Assign<string>
                {
                    DisplayName = "Student Discount is appplied",
                    To = discountCode,
                    Value = new InArgument<string>("STD")
                },
                Next = discountCodeSwitch
            };

            FlowStep marriedWithNoChildDiscount = new FlowStep
            {
                Action = new Assign<string>
                {
                    DisplayName = "Married with no child Discount is applied",
                    To = discountCode,
                    Value = new InArgument<string>("MNC")
                },
                Next = discountCodeSwitch
            };

            FlowStep marriedWithChildDiscount = new FlowStep
            {
                Action = new Assign<string>
                {
                    DisplayName = "Married with child Discount is 15%",
                    To = discountCode,
                    Value = new InArgument<string>("MWC")
                },
                Next = discountCodeSwitch
            };


            FlowDecision singleFlowDecision = new FlowDecision
            {
                Condition = ExpressionServices.Convert<bool>((ctx) => isStudent.Get(ctx)),
                True = studentDiscount,
                False = noDiscount,
            };

            FlowDecision marriedFlowDecision = new FlowDecision
            {
                Condition = ExpressionServices.Convert<bool>((ctx) => haveChild.Get(ctx)),
                True = marriedWithChildDiscount,
                False = marriedWithNoChildDiscount,
            };

            FlowDecision startNode = new FlowDecision
            {
                Condition = ExpressionServices.Convert<bool>((ctx) => isMarried.Get(ctx)),
                True = marriedFlowDecision,
                False = singleFlowDecision
            };

            return new Flowchart()
            {
                DisplayName = "Auto insurance discount calculation",
                Variables = { discount, isMarried, isStudent, haveChild, discountCode },

                StartNode = startNode,
                Nodes =
                            {
                                startNode,
                                marriedFlowDecision,
                                singleFlowDecision,
                                marriedWithChildDiscount,
                                marriedWithNoChildDiscount,
                                studentDiscount,
                                noDiscount,
                                discountCodeSwitch,
                                applyBaseDiscount,
                                applyDefaultDiscount,
                                applyPremiumDiscount,
                                applyStandardDiscount,
                                printDiscount,
                                returnDiscountCode
                            }
            };
        }

        private Func<Activity> _implementation;
        protected override Func<Activity> Implementation
        {
            get
            {
                return _implementation ?? (_implementation = GetImplementation);
            }

            set { throw new NotSupportedException(); }
        }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("DiscountCode", typeof(string), ArgumentDirection.Out));
            metadata.Bind(this.DiscountCode, runtimeArguments[0]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }
    }
}

