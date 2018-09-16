    using CoreWf;
    using CoreWf.Statements;
using Microsoft.Samples.NestedTransactionScope;
using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

namespace ImperativeTestCases
{
    /// <summary>
    /// Sample DynamicActivity based on https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/creating-an-activity-at-runtime-with-dynamicactivity
    /// </summary>
    public class DynamicDemo
    {
        public static void RunDynamicWriteLine()
        {
            //Define the input argument for the activity  
            var textOut = new InArgument<string>();
            //Create the activity, property, and implementation  
            Activity dynamicWorkflow = new DynamicActivity()
            {
                Properties =
                {
                    new DynamicActivityProperty
                    {
                        Name = "Text",
                        Type = typeof(InArgument<String>),
                        Value = textOut
                    }
                },
                Implementation = () => new Sequence()
                {
                    Activities =
                    {
                        new WriteLine()
                        {
                            Text = new InArgument<string>(env => textOut.Get(env))
                        }
                    }
                }
            };
            //Execute the activity with a parameter dictionary  
            WorkflowInvoker.Invoke(dynamicWorkflow, new Dictionary<string, object> { { "Text", "Hello World!" } });
        }

        public static void ParameterlessDelay()
        {
            //Define the input argument for the activity  
            var textOut = new InArgument<string>();
            //Create the activity, property, and implementation  
            Activity dynamicWorkflow = new DynamicActivity()
            {
                Implementation = () => new Sequence()
                {
                    Activities =
                    {
                        new Delay()
                        {
                            Duration = new InArgument<TimeSpan>(new TimeSpan(200))
                        },
                        new WriteLine()
                        {
                            Text = new InArgument<string>("Delay was successful.")
                        }
                    }
                }
            };
            //Execute the activity with a parameter dictionary  
            WorkflowInvoker.Invoke(dynamicWorkflow);
        }

        public static void ParameterDelay()
        {
            //Define the input argument for the activity  
            var delayValue = new InArgument<TimeSpan>();
            //Create the activity, property, and implementation  
            Activity dynamicWorkflow = new DynamicActivity()
            {
                Properties =
                {
                    new DynamicActivityProperty
                    {
                        Name = "DelayDuration",
                        Type = typeof(InArgument<TimeSpan>),
                        Value = delayValue
                    }
                },
                Implementation = () => new Sequence()
                {
                    Activities =
                    {
                        new Delay()
                        {
                            Duration = new InArgument<TimeSpan>(env => delayValue.Get(env))
                        },
                        new WriteLine()
                        {
                            Text = new InArgument<string>("Delay was successful.")
                        }
                    }
                }
            };
            //Execute the activity with a parameter dictionary  
            WorkflowInvoker.Invoke(dynamicWorkflow, new Dictionary<string, object> { { "DelayDuration", new TimeSpan(200) } });
        }

        public static void ParameterAssign()
        {
            //Define the input argument for the activity  
            var inputValue = new InArgument<int>();
            var outputValue = new OutArgument<int>();
            //Create the activity, property, and implementation  
            Activity dynamicWorkflow = new DynamicActivity()
            {
                Properties =
                {
                    new DynamicActivityProperty
                    {
                        Name = "InputInteger",
                        Type = typeof(InArgument<int>),
                        Value = inputValue
                    },
                    new DynamicActivityProperty
                    {
                        Name = "OutputInteger",
                        Type = typeof(OutArgument<int>),
                        Value = outputValue
                    },
                },
                Implementation = () => new Sequence()
                {
                    Activities =
                    {
                        new Assign()
                        {
                            To = new OutArgument<int>(env => outputValue.Get(env)),
                            Value = new InArgument<int>(env => inputValue.Get(env) )
                        },
                        new WriteLine()
                        {
                            Text = new InArgument<string>("Assign was successful")
                        }
                    }
                }
            };
            //Execute the activity with a parameter dictionary  
            var o = WorkflowInvoker.Invoke(dynamicWorkflow, new Dictionary<string, object> { { "InputInteger", 42 } });
            foreach (var kvp in o)
            {
                Console.WriteLine(kvp.Key + " : " + kvp.Value.ToString());
            }
            if(o.TryGetValue("OutputInteger", out object value))
            {
                int ReturnedInt = (int)value;
                if (ReturnedInt == 42) Console.WriteLine("Output successful expected answer was 42"); 
            }
        }


        public static void TransactionScopeTest()
        {
            Activity transactionTest = new DynamicActivity()
            {
                Implementation = () => new Sequence()
                {
                    Activities =
                    {
                        new WriteLine { Text = "    Begin workflow" },
                        new TransactionScope
                        {
                            Body = new Sequence()
                            {
                                Activities = {
                                    new WriteLine { Text = "    Begin TransactionScope" },
                                    new PrintTransactionId(),
                                    new TransactionScopeTest(),
                                    new WriteLine { Text = "    End TransactionScope" },
                                },
                            },
                        },
                        new WriteLine { Text = "    End workflow" }
                    }
                }
            };
            WorkflowInvoker.Invoke(transactionTest);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("------------- ParameterlessDelay ------------- ");
            DynamicDemo.ParameterlessDelay();

            Console.WriteLine("------------- Hello World ------------- ");
            try
            {
                DynamicDemo.RunDynamicWriteLine();
            }
            catch (CoreWf.InvalidWorkflowException iwe)
            {
                Console.WriteLine("Invalid Workflow WriteLine" + iwe.ToString());
            }
            Console.WriteLine("------------- ParameterDelay ------------- ");
            try
            {
                DynamicDemo.ParameterDelay();
            }
            catch (CoreWf.InvalidWorkflowException iwe)
            {
                Console.WriteLine("Invalid Workflow ParameterDelay" + iwe.ToString());
            }
            Console.WriteLine("------------- ParameterDelayAssigned ------------- ");
            try
            {
                DynamicDemo.ParameterAssign();
            }
            catch (CoreWf.InvalidWorkflowException iwe)
            {
                Console.WriteLine("Invalid Workflow ParameterDelay" + iwe.ToString());
            }
            Console.WriteLine("------------- TransactionScopeTest ------------- ");
            try
            {
                DynamicDemo.TransactionScopeTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("------------- Done ------------- ");
            Console.ReadLine();
        }
    }
}
