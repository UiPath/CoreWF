// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common;
using Xunit;

namespace TestCases.Activities
{
    public class MethodInvokeTests : IDisposable
    {
        public void Dispose()
        {
        }

        [Fact]
        public void SimpleMethodInvoke()
        {
            //  SimpleMethodInvokeSimple method invoke call with a simple string MethodName(int arg)
            //  Test case description:
            //  Simple method invoke call with a simple string MethodName(int arg)

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 123)
                },
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        [Fact]
        public void StaticMethodWithParams()
        {
            //  StaticMethodWithParamsStatic method with parameters
            //  Test case description:
            //  Static method with parameters

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStaticMethod"))
            {
                TargetType = typeof(NonGenericClass),
                Arguments =
                {
                    new TestArgument<double>(Direction.In, "input", 2.342)
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        [Fact]
        public void GenericArgMethod()
        {
            //  1 generic args for method ( 5 at least?)
            //  Test case description:
            //  More than 1 generic args for method ( 5 at least?)

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleGenericMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "input", 2342)
                },
                GenericTypeArguments =
                {
                    typeof(int)
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        [Fact]
        //[HostWorkflowAsWebService]
        public void RefParamMethod()
        {
            //  RefParamMethodMethod that has ref parameters
            //  Test case description:
            //  Method that has ref parameters
            Variable<List<string>> var = new Variable<List<string>>("var", context => new List<string> { "e", "life", "on", "a lan" });

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleRefMethod"))
                    {
                        TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                        Arguments =
                        {
                            new TestArgument<List<string>>(Direction.InOut, "input", var)
                        }
                    },
                    new TestIf(HintThenOrElse.Then)
                    {
                        ConditionExpression = ((env) => ((List<string>)var.Get(env)).Contains("nofile")),
                        ThenActivity = new TestProductWriteline
                        {
                            Text = " ref arg set correctly "
                        }
}
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        //[HostWorkflowAsWebService]
        public void OutParamMethod()
        {
            //  RefParamMethodMethod that has ref parameters
            //  Test case description:
            //  Method that has ref parameters
            Variable<List<string>> var = new Variable<List<string>>("var", context => new List<string>());

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleOutMethod"))
                    {
                        TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                        Arguments =
                        {
                            new TestArgument<List<string>>(Direction.Out, "input", var)
                        }
                    },
                    new TestIf(HintThenOrElse.Then)
                    {
                        ConditionExpression = ((env) => ((List<string>)var.Get(env)).Contains("a lan")),
                        ThenActivity = new TestProductWriteline
                        {
                            Text = " out arg set correctly "
                        }
}
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void CallSynchAsynch()
        {
            //  CallSynchAsynchCall a synch method asynch
            //  Test case description:
            //  Call a synch method asynch

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 123)
                },
                RunAsynchronously = true
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        [Fact]
        public void CallOverloadedWriteLine()
        {
            try
            {
                TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(System.Console).GetMethod("WriteLine"))
                {
                    TargetType = typeof(System.Console),
                    Arguments =
                    {
                        new TestArgument<string>(Direction.In, "value", "dangerous... dangerous... ")
                    },
                };
                throw new TestCaseFailedException("Expecting CoreWf.ValidationException, but exception is not thrown");
            }
            catch (Exception exception)
            {
                Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
                exceptionProperty.Add("Message", string.Format("Ambiguous match found."));

                ExceptionHelpers.ValidateException(exception, typeof(System.Reflection.AmbiguousMatchException), exceptionProperty);
            }
        }

        /// <summary>
        /// For a myObjInst.myMethod() call no target obj set- val err
        /// </summary>        
        [Fact]
        public void TargetObjectNotSet()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod("NoTargetObjectMethod", typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                // Not setting TargetObject
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 123)
                }
            };

            string exceptionMessage = string.Format(ErrorStrings.OneOfTwoPropertiesMustBeSet, "TargetObject", "TargetType", "InvokeMethod", "NoTargetObjectMethod");
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(exceptionMessage, simpleMethodInvoke.ProductActivity, false));

            TestRuntime.ValidateWorkflowErrors(simpleMethodInvoke, constraints, exceptionMessage);
        }

        /// <summary>
        /// For a static method call target type not set – val err
        /// </summary>        
        [Fact]
        public void TargetTypeNotSet()
        {
            //failing 76549
            //  TargetTypeNotSetFor a static method call target type not set – val err
            //  Test case description:
            //  For a static method call target type not set – val err

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod("NoTargetTypeMethod", typeof(NonGenericClass).GetMethod("SimpleStaticMethod"))
            {
                // Not setting TargetType
                Arguments =
                {
                    new TestArgument<double>(Direction.In, "input", 2.342)
                }
            };

            try
            {
                TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);

                // expecting an exception, but exception not received.
                throw new TestCaseFailedException("Expecting CoreWf.ValidationException, but exception is not thrown");
            }
            catch (Exception exception)
            {
                //Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
                //exceptionProperty.Add("Message", string.Format(ErrorStrings.MethodInvokePropertiesNotSpecified, "NoTargetTypeMethod", "TargetObject", "TargetType"));

                //ExceptionHelpers.ValidateException(exception, typeof(ValidationException), exceptionProperty);
                Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// For a method with return type result not set – val err?
        /// </summary>        
        [Fact]
        public void ResultNotSet()
        {
            //  ResultNotSetFor a method with return type result not set 
            //  Test case description:
            //  For a method with return type result not set 

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 11)
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// For a method with parameters, dont add any parameters – val err
        /// </summary>        
        [Fact]
        public void ParametersEmpty()
        {
            //  ParametersEmptyFor a method with parameters, dont add any parameters – val err
            //  Test case description:
            //  For a method with parameters, dont add any parameters – val err

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                DisplayName = "SimpleInvokeMethod"

                // No Parameter set.
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "SimpleStringMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Invoke a method with the wrong Argument
        /// </summary>        
        [Fact]
        public void NonMatchingTypesForArguments()
        {
            string arg = "Hello Peoples!";

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "abc", arg)
                }
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "SimpleStringMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Don’t set method name val err
        /// </summary>        
        [Fact]
        public void MethodNameNotSet()
        {
            //  MethodNameNotSet Don’t set method name val err
            //  Test case description:
            //  Don’t set method name val err

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod("MethodNoName", typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 123)
                },
                // Set the method name to null
                MethodName = null
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "MethodName", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Method that has same type different name parameters
        /// </summary>        
        [Fact]
        public void MethodSameTypeDiffNameParams()
        {
            //  MethodSameTypeDiffNameParamsMethod that has same type different name parameters
            //  Test case description:
            //  Method that has same type different name parameters

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SameTypeDiffNameMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "integer1", 15),
                    new TestArgument<int>(Direction.In, "integer2", 25)
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// For a generic method call don’t set generic args
        /// </summary>        
        [Fact]
        public void GenericArgsNotSet()
        {
            //  GenericArgsNotSetFor a generic method call don’t set generic args
            //  Test case description:
            //  For a generic method call don’t set generic args
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleGenericMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "input", 2342)
                },

                DisplayName = "GenericArgMethod"

                // Don't set Generic Type Arguments
            };

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, String.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "SimpleGenericMethod", simpleMethodInvoke.ProductActivity.DisplayName));
        }

        private void MyAsyncMethod()
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(i.ToString());
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Call an asynch method synch way
        /// </summary>        
        [Fact]
        public void CallAsynchSynch()
        {
            //  CallAsynchSynchCall an asynch method synch way
            //  Test case description:
            //  Call an asynch method synch way
            //  When run in Parallel, the method will be run synchronously, so when completion Condition
            //  is set to true, InvokeMethod will run -> Complete and then Parallel will complete.

            //cannot xaml roundtrip Action
            //TestParameters.SetParameter("DisableXamlRoundTrip", "True");

            TestParallel par = new TestParallel("ParallelTest")
            {
                Branches =
                {
                    new TestInvokeMethod(typeof(Action).GetMethod("Invoke"))
                    {
                        TargetObject = new TestArgument<Action>(Direction.In, "TargetObject", (context => new Action(() => MyAsyncMethod()))),
                        RunAsynchronously = false,
                    },
                    new TestWriteLine("WritingInBranch")
                    {
                        Message = "Writing",
                        HintMessage = "Writing",
                    },
                    new TestWriteLine("WritingInBranch2")
                    {
                        Message = "Writing",
                        HintMessage = "Writing",
                    },
                },
                // only one method is needed to complete
                CompletionCondition = true,
                HintNumberOfBranchesExecution = 1
            };

            TestRuntime.RunAndValidateWorkflow(par);
        }

        /// <summary>
        /// Set target object for static method – val.err
        /// </summary>        
        [Fact]
        public void SetTargetObjForStaticMethod()
        {
            //SetTargetObjForStaticMethodSet target object for static method
            //Test case description:
            //Set target object for static method

            TestInvokeMethod simpleStaticMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStaticMethod"))
            {
                TargetType = typeof(NonGenericClass),
                Arguments =
                {
                    // NonGenericClass.SimpleStaticMethod expects a parameter of type "double".
                    // But due to https://github.com/dotnet/wf/issues/78 - limitations in GetMethod, the
                    // check for the ability to convert an int to a double does not happen.
                    // So changing the argument passed to a double, rather than marking this test as Skip
                    //new TestArgument<int>(Direction.In, "input", 2342)
                    new TestArgument<double>(Direction.In, "input", 2342)
                },
            };

            TestRuntime.RunAndValidateWorkflow(simpleStaticMethodInvoke);
        }

        [Fact]
        public void SetTargetTypeForNonStaticMethod()
        {
            //  NoDefConstructorNoTargetObject(  If )we are creating instance of target object ourselves when target type is set  and no target object is set for a class without def contructor 
            //  Test case description:
            //  For a non static method, set the target type to call this method

            TestInvokeMethod simpleStaticMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                    {
                        new TestArgument<int>(Direction.In, "abc", 1234)
                    },
            };

            TestRuntime.RunAndValidateWorkflow(simpleStaticMethodInvoke);
        }

        /// <summary>
        /// Call private method
        /// </summary>        
        [Fact]
        public void CallPrivateMethod()
        {
            //  CallPrivateMethodCall private method
            //  Test case description:
            //  Call private method
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),

                // setting the constructor to NoParamMethod because if setting it to PrivateMethod, it would not
                // find the method and construction fails.
                MethodName = "PrivateMethod",
                DisplayName = "PrivateMethodInvoking"
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "PrivateMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Call protected method
        /// </summary>        
        [Fact]
        public void CallProtectedMethod()
        {
            //  CallProtectedMethodCall protected method
            //  Test case description:
            //  Call protected method
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),

                // setting the constructor to NoParamMethod because if setting it to PrivateMethod, it would not
                // find the method and construction fails.
                MethodName = "ProtectedMethod",
                DisplayName = "ProtectedMethodInvoking"
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "ProtectedMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Call referenced user dll’s public method
        /// </summary>        
        // Skipped due to Test.Common.TestObjects.Utilities.DirectoryAssistance usage.
        //[Fact]
        //public void CallReferencedUserDllsMethod()
        //{
        //    //  CallReferencedUserDllsMethodCall referenced user dll’s public method
        //    //  Test case description:
        //    //  Call referenced user dll’s public method

        //    string ProductRootDir = Test.Common.TestObjects.Utilities.DirectoryAssistance.GetProductRootDirectory();

        //    Variable<string> myRetString = VariableHelper.Create<string>("myRetString");

        //    TestInvokeMethod structMethod =
        //        new TestInvokeMethod(typeof(Test.Common.TestObjects.Utilities.DirectoryAssistance).GetMethod("GetProductRootDirectory"))
        //    {
        //        TargetType = typeof(Test.Common.TestObjects.Utilities.DirectoryAssistance),
        //        MethodName = "GetProductRootDirectory",
        //    };
        //    structMethod.SetResultVariable<string>(myRetString);

        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables =
        //        {
        //            myRetString
        //        },
        //        Activities =
        //        {
        //            structMethod,
        //            new TestWriteLine()
        //            {
        //                MessageExpression = ((env)=>myRetString.Get(env)),
        //                HintMessage = ProductRootDir
        //            }
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        /// <summary>
        /// Call referenced system dll’s public method
        /// </summary>        
        [Fact]
        public void CallReferencedSystemDllsMethod()
        {
            //  CallReferencedSystemDllsMethodCall referenced system dll’s public method
            //  Test case description:
            //  Call referenced system dll’s public method

            string copiedString = "MyString";

            Variable<string> myRetString = VariableHelper.Create<string>("myRetString");

            TestInvokeMethod structMethod = new TestInvokeMethod(typeof(String).GetMethod("Copy"))
            {
                TargetType = typeof(string),
                MethodName = "Copy",
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "str", copiedString)
                },
            };
            structMethod.SetResultVariable<string>(myRetString);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myRetString
                },
                Activities =
                {
                    structMethod,
                    new TestWriteLine()
                    {
                        MessageExpression = ((env)=>myRetString.Get(env)),
                        HintMessage = copiedString
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        internal void myMethod()
        {
            Console.WriteLine("I'm in InternalMethod");
        }

        /// <summary>
        /// Call internal method from same dll
        /// </summary>        
        [Fact]
        public void CallInternal()
        {
            //  CallInternalCall internal method from same dll
            //  Test case description:
            //  Call internal method from same dll

            TestInvokeMethod internalMethod = new TestInvokeMethod("myMethod");
            internalMethod.TargetObject = new TestArgument<MethodInvokeTests>(Direction.In, "TargetObject", (context => new MethodInvokeTests()));
            internalMethod.MethodName = "myMethod";

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "MethodInvokeTests", "instance", "myMethod", internalMethod.ProductActivity.DisplayName);
            TestRuntime.ValidateInstantiationException(internalMethod, error);
        }

        /// <summary>
        /// Call a method that is defined in a struct with target object
        /// </summary>        
        [Fact]
        public void CallStructMethod()
        {
            //  CallStructMethodCall a method that is defined in a struct with target object
            //  Test case description:
            //  Call a method that is defined in a struct with target object

            TestInvokeMethod structMethod = new TestInvokeMethod(typeof(StructClass).GetMethod("SimpleStructMethod"))
            {
                TargetObject = new TestArgument<StructClass>(Direction.In, "TargetObject", (context => new StructClass())),
                MethodName = "SimpleStructMethod"
            };

            TestRuntime.RunAndValidateWorkflow(structMethod);
        }

        [Fact]
        public void CallStructMethodWithTargetType()
        {
            //  CallStructMethodWithTargetTypeCall a method that is defined in a struct with target type set not target object
            //  Test case description:
            //  Call a method that is defined in a struct with target type set not target object

            TestInvokeMethod structMethod = new TestInvokeMethod(typeof(StructClass).GetMethod("SimpleStructMethod"))
            {
                TargetType = typeof(StructClass),
                MethodName = "SimpleStructMethod"
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "StructClass", "static", "SimpleStructMethod", structMethod.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(structMethod, error);
        }



        /// <summary>
        /// Overloaded methods with same param names and param count but param types are different. (i.e. method1(int str, string i) method2 (int I, string str))
        /// Repro workflow:
        ///             Variable<object> hello = new Variable<object> { Default = "hello" };
        ///             Variable<Array> array = new Variable<Array>()
        ///             {
        ///                 Default = Array.CreateInstance(typeof(string), 5)
        ///             };
        ///             WorkflowElement workflow = new Sequence
        ///             {
        ///                 Variables = { hello , array },
        ///                 Activities =
        ///                 {
        ///                     new MethodInvoke()
        ///                     {
        ///                         DisplayName = "public void SetValue(object value, int index)",
        ///                         MethodName = "SetValue",
        ///                         TargetObject = new InArgument<Array>(env => array.Get(env)),
        ///                         Parameters =
        ///                         {
        ///                             {"value", new InArgument<object>(hello)},
        ///                             {"index", new InArgument<int>(0)}
        ///                         }
        ///                     }
        ///                 }
        ///             };
        /// </summary>        
        [Fact]
        public void OverloadedMethodsWithSameParamNames()
        {
            //  OverloadedMethodsWithSameParamNamesOverloaded methods with same param names and param count but param types are different. (i.e. method1(int str, string i) method2 (int I, string str))
            //  Test case description:
            //  Overloaded methods with same param names and param count but param types are different. (i.e.
            //  method1(int str, string i) method2 (int I, string str))
            try
            {
                TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("OverloadingMethod"))
                {
                    TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                    Arguments =
                    {
                        new TestArgument<int>(Direction.In, "first", 123),
                        new TestArgument<string>(Direction.In, "second", "Hello"),
                    }
                };

                // expecting an exception, but exception not received.
                throw new TestCaseFailedException("Expecting CoreWf.ValidationException, but exception is not thrown");
            }
            catch (Exception exception)
            {
                Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
                exceptionProperty.Add("Message", string.Format("Ambiguous match found."));

                ExceptionHelpers.ValidateException(exception, typeof(System.Reflection.AmbiguousMatchException), exceptionProperty);
            }
        }

        /// <summary>
        /// Method without parameter and return value.
        /// </summary>        
        [Fact]
        public void VoidMethodWithoutParam()
        {
            //  VoidMethodWithoutParam without parameter and return value.
            //  Test case description:
            //  Method without parameter and return value.

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Using a method with \
        /// Using a method with "params" but passing no argument
        /// </summary>        
        [Fact]
        public void VoidUsingParamsArgButNoArgPassedIn()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MethodWithParams"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Using a method with \
        /// Using a method with "params" and one argument is passed in
        /// </summary>        
        [Fact]
        public void VoidUsingParamsArgOneArgPassedIn()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MethodWithParams"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "first", "Bob")
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Using a method with \
        /// Using a method with "params" and multiple argument are passed in
        /// </summary>        
        [Fact]
        public void VoidUsingParamsArgMultipleArgsPassedIn()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MethodWithParams"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "first", "Microsoft"),
                    new TestArgument<string>(Direction.In, "second", "Bob"),
                    new TestArgument<string>(Direction.In, "third", "Omri"),
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Using a method with \
        /// Using a method with "params" and an array is passed in
        /// </summary>        
        [Fact]
        public void VoidUsingParamsArrayPassedIn()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MethodWithParams"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<string[]>(Direction.In, "names", context => new string[] { "Microsoft", "Bob", "Omrie" })
                }
            };
            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Have a Mismatching TargetObject and TargetType
        /// Set both TargetType and TargetObject that don't match
        /// </summary>        
        [Fact]
        public void TargetTypeAndTargetObjectDontMatch()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                TargetType = typeof(GenericClass<string>),
            };

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, String.Format(ErrorStrings.TargetTypeAndTargetObjectAreMutuallyExclusive, "InvokeMethod", simpleMethodInvoke.ProductActivity.DisplayName));
        }

        /// <summary>
        /// Invoke an Extension method. The only way is through Setting the TargetType
        /// </summary>        
        [Fact]
        public void InvokingExtensionMethod()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(ExtensionMethodClass).GetMethod("StringExtensionMethod"))
            {
                TargetType = typeof(ExtensionMethodClass),
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "type", "Hello")
                }
            };

            TestRuntime.RunAndValidateWorkflow(simpleMethodInvoke);
        }

        /// <summary>
        /// Can not invoke an extension method directly.
        /// </summary>        
        [Fact]
        public void InvokingExtensionMethodNegative()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(ExtensionMethodClass).GetMethod("StringExtensionMethod"))
            {
                TargetObject = new TestArgument<string>(Direction.In, "TargetObject", (context => "Hello")),
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "String", "instance", "StringExtensionMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(simpleMethodInvoke, error);
        }

        /// <summary>
        /// Invoking a non generic method with a Return Type
        /// </summary>        
        [Fact]
        public void NonGenericWithReturnAndParams()
        {
            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SimpleStringMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "abc", 5),
                },
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = "im done"
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a Generic method with a Return Type
        /// Generic object with return and parameters of generic type
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void GenericWithReturnAndParams()
        {
            string myString = "I am getting invoked!";

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(GenericClass<string>).GetMethod("GenericMethodWithReturnType"))
            {
                TargetObject = new TestArgument<GenericClass<string>>(Direction.In, "TargetObject", (context => new GenericClass<string>())),
                Arguments =
                {
                    new TestArgument<string>(Direction.In, "input", myString),
                },
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = myString
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a static Generic Method with No Arguments
        /// </summary>        
        [Fact]
        public void GenericStaticMethodWithNoArgs()
        {
            string myString = "I have no Parameters and I am Staticky!";

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(GenericClass<string>).GetMethod("NoParamMethodStatic"))
            {
                TargetType = typeof(GenericClass<string>),
                GenericTypeArguments =
                {
                    typeof(string)
                }
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = myString
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a Generic Method with No Arguments
        /// </summary>        
        [Fact]
        public void NoParamMethodNonStatic()
        {
            string myString = "I have no Parameters and I am Not Staticky!";

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(GenericClass<string>).GetMethod("NoParamMethodNonStatic"))
            {
                TargetObject = new TestArgument<GenericClass<string>>(Direction.In, "TargetObject", (context => new GenericClass<string>())),
                GenericTypeArguments =
                {
                    typeof(string)
                }
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = myString
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a Method with multiple generic types
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void MultipleGenericTypeMethod()
        {
            int myInt = 12398545;
            double myDbl = 23424.3423;

            string returnStr = myInt.ToString() + " " + myDbl.ToString();

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MultipleGenericMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "input1", myInt),
                    new TestArgument<double>(Direction.In, "input2", myDbl),
                },
                GenericTypeArguments =
                {
                    typeof(int),
                    typeof(double),
                }
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = returnStr
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a Method with Generic args added in incorrect order to the dictionary – val err
        /// Generic args added in incorrect order to the dictionary – val err
        /// </summary>        
        [Fact]
        public void GenericArgsDiffOrder()
        {
            int myInt = 12398545;
            double myDbl = 23424.3423;

            string returnStr = myInt.ToString() + " " + myDbl.ToString();

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("MultipleGenericMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "input1", myInt),
                    new TestArgument<double>(Direction.In, "input2", myDbl),
                },
                GenericTypeArguments =
                {
                    typeof(double),
                    typeof(int),
                }
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = returnStr
                    }
                }
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "MultipleGenericMethod", simpleMethodInvoke.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(seq, error);
        }

        /// <summary>
        /// Invoking a Method with Generic args of the same type
        /// </summary>        
        [Fact]
        public void SameTypeGenericArgs()
        {
            double myDouble1 = 235.453;
            double myDouble2 = 12398545.325;
            double myDouble3 = 1239.54365;

            string returnStr = myDouble1.ToString() + " " + myDouble2.ToString() + " " + myDouble3.ToString();

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("SameTypeGenericArgs"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                Arguments =
                {
                    new TestArgument<double>(Direction.In, "input1", myDouble1),
                    new TestArgument<double>(Direction.In, "input2", myDouble2),
                    new TestArgument<double>(Direction.In, "input3", myDouble3),
                },
                GenericTypeArguments =
                {
                    typeof(double),
                }
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = returnStr
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoking a Method from a generic Class with args of the same type
        /// </summary>        
        [Fact]
        public void SameTypeGenericClassArgs()
        {
            int myInt1 = 235;
            int myInt2 = 12398545;
            int myInt3 = 1239;

            string returnStr = myInt1.ToString() + " " + myInt2.ToString() + " " + myInt3.ToString();

            TestInvokeMethod simpleMethodInvoke = new TestInvokeMethod(typeof(GenericClass<int>).GetMethod("SameTypeGenericArgs"))
            {
                TargetObject = new TestArgument<GenericClass<int>>(Direction.In, "TargetObject", (context => new GenericClass<int>())),
                Arguments =
                {
                    new TestArgument<int>(Direction.In, "input1", myInt1),
                    new TestArgument<int>(Direction.In, "input2", myInt2),
                    new TestArgument<int>(Direction.In, "input3", myInt3),
                },
            };

            Variable<string> myReturnValue = new Variable<string> { Name = "myVar" };

            simpleMethodInvoke.SetResultVariable<string>(myReturnValue);

            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    myReturnValue
                },
                Activities =
                {
                    simpleMethodInvoke,

                    new TestWriteLine()
                    {
                        MessageVariable = myReturnValue,
                        HintMessage = returnStr
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Negative Test Case for Using TargetType to invoke Non Static Types
        /// </summary>        
        [Fact]
        public void InvokeNonStaticMethodsWithTargetTypeSet()
        {
            TestInvokeMethod classMethod = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamMethod"))
            {
                TargetType = typeof(NonGenericClass),
                MethodName = "NoParamMethod"
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "static", "NoParamMethod", classMethod.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(classMethod, error);
        }

        /// <summary>
        /// Negative Test Case for Using TargetType to invoke Static Types
        /// </summary>        
        [Fact]
        public void InvokeStaticMethodsWithTargetObejectSet()
        {
            TestInvokeMethod classMethod = new TestInvokeMethod(typeof(NonGenericClass).GetMethod("NoParamStaticMethod"))
            {
                TargetObject = new TestArgument<NonGenericClass>(Direction.In, "TargetObject", (context => new NonGenericClass())),
                MethodName = "NoParamStaticMethod"
            };

            string error = string.Format(ErrorStrings.PublicMethodWithMatchingParameterDoesNotExist, "NonGenericClass", "instance", "NoParamStaticMethod", classMethod.ProductActivity.DisplayName);

            TestRuntime.ValidateInstantiationException(classMethod, error);
        }

        // Skipping due to VisualBasicValue usage
        //[Fact]
        //public void DifferentArguments()
        //{
        //    //Testing Different argument types for If.Condition
        //    // DelegateInArgument
        //    // DelegateOutArgument
        //    // Activity<T>
        //    // Variable<T> and Expression is already implemented.

        //    DelegateInArgument<string> delegateInArgument = new DelegateInArgument<string>("Input");
        //    DelegateOutArgument<string> delegateOutArgument = new DelegateOutArgument<string>("Output");

        //    TestCustomActivity<InvokeFunc<string, string>> invokeFunc = TestCustomActivity<InvokeFunc<string, string>>.CreateFromProduct(
        //        new InvokeFunc<string, string>
        //         {
        //             Argument = "PassedInValue",
        //             Func = new ActivityFunc<string, string>
        //             {
        //                 Argument = delegateInArgument,
        //                 Result = delegateOutArgument,
        //                 Handler = new CoreWf.Statements.Sequence
        //                 {
        //                     DisplayName = "Sequence1",
        //                     Activities =
        //                     {
        //                         new InvokeMethod<string>
        //                         {
        //                             DisplayName = "InvokeMethod1",
        //                             TargetObject = new InArgument<string>( delegateInArgument),
        //                             Result = delegateOutArgument,
        //                             MethodName = "ToUpper",
        //                         },
        //                         new Test.Common.TestObjects.CustomActivities.WriteLine{DisplayName = "W1", Message = new VisualBasicValue<string>("Output") },
        //                         new InvokeMethod<string>
        //                         {
        //                             DisplayName = "InvokeMethod2",
        //                             TargetObject = new InArgument<string>( delegateOutArgument),
        //                             Result = delegateInArgument,
        //                             MethodName = "ToLower",
        //                         },
        //                         new WriteLine{ DisplayName = "W2", Message = new VisualBasicValue<string>("Input") },
        //                     }
        //                 }
        //             }
        //         }
        //       );

        //    TestSequence sequenceForTracing = new TestSequence
        //    {
        //        DisplayName = "Sequence1",
        //        Activities =
        //        {
        //            new TestInvokeMethod{ DisplayName = "InvokeMethod1"},
        //            new TestWriteLine{DisplayName = "W1", HintMessage = "PASSEDINVALUE"},
        //            new TestInvokeMethod{ DisplayName = "InvokeMethod2"},
        //            new TestWriteLine{DisplayName = "W2", HintMessage = "passedinvalue"},
        //        }
        //    };

        //    invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);
        //    TestRuntime.RunAndValidateWorkflow(invokeFunc);
        //}

        [Fact]
        public void InvokeBaseInterfaceMethod()
        {
            Variable<IDerived> var = new Variable<IDerived>("var", env => new DerivedClass());

            TestSequence sequence = new TestSequence()
            {
                Variables =
                {
                    var,
                },
                Activities =
                {
                    new TestInvokeMethod()
                    {
                        TargetObject = new TestArgument<IBase>(Direction.In, "name", env=>var.Get(env) as IBase),
                        MethodName = "MyMethod",
                    }
                },
            };

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        [Fact]
        public void InvokeMethodAsyncMismatch_Regress229446()
        {
            var workflow = new InvokeMethod<int>
            {
                TargetType = typeof(MethodInvokeTests),
                MethodName = "Hello",
                RunAsynchronously = true,
            };

            try
            {
                WorkflowInvoker.Invoke(workflow);
            }
            catch (InvalidWorkflowException)
            {
                //Log.Trace("Caught Expected InvalidWorkflowException.  Test Passed");
            }
            catch (NullReferenceException)
            {
                throw new TestCaseException("Unexpected NullReferenceException caught.  Expect InvalidWorkflowException");
            }
        }

        public delegate int HelloDelegate();
        public static IAsyncResult BeginHello()
        {
            HelloDelegate HelloDelegate = new HelloDelegate(Hello);
            return HelloDelegate.BeginInvoke(null, HelloDelegate);
        }

        // *** The return type is deliberately inconsistent here to trigger a validation error. It should have been int instead. ***
        public static string EndHello(IAsyncResult result)
        {
            return ((HelloDelegate)result.AsyncState).EndInvoke(result).ToString();
        }
        public static int Hello()
        {
            Thread.Sleep(1000);
            return 1;
        }
    }

    public static class ExtensionMethodClass
    {
        public static void StringExtensionMethod(this String type)
        {
            Console.WriteLine(type);
        }
    }

    public class NonGenericClass
    {
        public string SimpleStringMethod(int abc)
        {
            //Log.Info("in the simple method " + abc);
            return "im done";
        }
        public static Guid SimpleStaticMethod(double input)
        {
            //Log.Info("in the simple static method " + input);
            return new Guid();
        }
        public void SimpleRefMethod(ref List<string> input)
        {
            input.Add("file");
            input.Add("nofile");
            foreach (string str in input)
            {
                //Log.Info("in the simple ref method " + str);
            }
        }
        public void SimpleOutMethod(out List<string> input)
        {
            input = new List<string>();
            input.Add("e");
            input.Add("life");
            input.Add("on");
            input.Add("a lan");
            foreach (string str in input)
            {
                //Log.Info("in the simple ref method " + str);

            }
        }
        public void NoParamMethod()
        {
            //Log.Info("in the NoParamMethod");
        }
        public void SameTypeDiffNameMethod(int integer1, int integer2)
        {
            //Log.Info("in SameTypeDiffNameMethod with parameter integer1 = " + integer1);
            //Log.Info("in SameTypeDiffNameMethod with parameter integer2 = " + integer2);
        }

        public void OverloadingMethod(int first, string second)
        {
            //Log.Info("in OverloadingMethod1 with parameter first = " + first);
            //Log.Info("in OverloadingMethod1 with parameter second = " + second);
        }
        public void OverloadingMethod(string first, int second)
        {
            //Log.Info("in OverloadingMethod2 with parameter first = " + first);
            //Log.Info("in OverloadingMethod2 with parameter second = " + second);
        }
        private void PrivateMethod()
        {
            //Log.Info("I am a private method");
        }
        protected void ProtectedMethod()
        {
            //Log.Info("I am a protected method");
        }

        public void SimpleGenericMethod<T>(T input)
        {
            //Log.Info("in the simple method " + input.ToString());
        }

        public string MultipleGenericMethod<T, S>(T input1, S input2)
        {
            string output = input1.ToString() + " " + input2.ToString();
            return output;
        }

        public string SameTypeGenericArgs<T>(T input1, T input2, T input3)
        {
            string output = input1.ToString() + " " + input2.ToString() + " " + input3.ToString();
            return output;
        }

        public void MethodWithParams(params string[] input)
        {
            foreach (string str in input)
            {
                //Log.Info("My name is \"" + str + "\"  and I'm in an array");
            }
        }

        public static void NoParamStaticMethod()
        {
            //Log.Info("I am a static Method with No Parameters");
        }
    }

    public class GenericClass<T>
    {
        public void SimpleGenericMethod(T input)
        {
            //Log.Info("in the simple method " + input.ToString());
        }

        public T GenericMethodWithReturnType(T input)
        {
            //Log.Info("Getting Input " + input.ToString());
            return input;
        }

        public static string NoParamMethodStatic<S>()
        {
            return "I have no Parameters and I am Staticky!";
        }

        public string NoParamMethodNonStatic<S>()
        {
            return "I have no Parameters and I am Not Staticky!";
        }

        public string SameTypeGenericArgs(T input1, T input2, T input3)
        {
            string output = input1.ToString() + " " + input2.ToString() + " " + input3.ToString();
            return output;
        }
    }

    public struct StructClass
    {
        public void SimpleStructMethod()
        {
            //Log.Info("I am a simple struct method");
        }

        public static void SimpleStructStaticMethod()
        {
            //Log.Info("I am simple static method");
        }
    }

    public interface IBase
    {
        void MyMethod();
    }

    public interface IDerived : IBase
    {
        void SomeOtherMethod();
    }

    public class DerivedClass : IDerived
    {
        public void MyMethod()
        {
            //Log.Info("In MyMethod");
        }

        public void SomeOtherMethod()
        {
            //Log.Info("In SomeOtherMethod");
        }
    }
}
