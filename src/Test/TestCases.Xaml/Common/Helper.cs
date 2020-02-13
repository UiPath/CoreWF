using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCases.Xaml.Driver;

namespace TestCases.Xaml.Common
{
    public static class Helper
    {
        static string GetMethodFullName(MethodBase testMethod)
        {
            return testMethod.DeclaringType.FullName + "." + testMethod.Name;
        }

        public static string GetInstanceIDPrefix(Type type)
        {
            return type.FullName + ".Instance";
        }

        public static TestCaseInfo GetInstance(List<Type> types, string instanceID)
        {
            List<TestCaseInfo> testCases = GetInstances(types);
            return GetInstance(testCases, instanceID);
        }

        public static TestCaseInfo GetInstance(ICollection<TestCaseInfo> testCases, string instanceID)
        {
            foreach (TestCaseInfo testCase in testCases)
            {
                if (testCase.TestID == instanceID)
                {
                    return testCase;
                }
            }

            throw new ArgumentException(string.Format("[Helper] can not find the instance '{0}'", instanceID));
        }

        public static List<TestCaseInfo> GetInstances(List<Type> types)
        {
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            foreach (Type type in types)
            {
                //ToDo: verify and log if type has GetTestCases implemented.
                //ToDo: if the instance does not have the GetTestCases method, using instance creator to create objects.
                //      It must be reproducible according to a seed. The ToString() must give an unique id that can identify the object.
                MethodInfo testMethod = type.GetMethod(Global.GetTestCasesMethodName);

                if (testMethod == null)
                {
                    Logger.Trace(GetMethodFullName(MethodInfo.GetCurrentMethod()), "Method '{0}' is not defined in type '{1}'.", Global.GetTestCasesMethodName, type.FullName);
                    continue;
                }
                testCases.AddRange(testMethod.Invoke(null, null) as IEnumerable<TestCaseInfo>);
            }

            return testCases;
        }

        //public static void GenerateTestCases(AddTestCaseEventHandler addCase, List<Type> testTypes, MethodBase generatorMethod)
        //{
        //    GenerateTestCases(addCase, Helper.GetInstances(testTypes), generatorMethod);
        //}

        //public static void GenerateTestCases(AddTestCaseEventHandler addCase, ICollection<TestCaseInfo> testCaseInfos, MethodBase generatorMethod)
        //{
        //    // unfortunately, TestHost requires that the generator method and generated method name to be different
        //    // The convention used here is - to append "Method" to the generator method name for generated method name
        //    MethodInfo testMethod = generatorMethod.DeclaringType.GetMethod(generatorMethod.Name + "Method", new Type[] { typeof(string) });

        //    // ToDo: Unfortunately, the DeclaringType of DynamicMethod is always null, which fails AddTestCaseEventHandler.
        //    //       Investigate if there is some way to get around this. If this can work, it makes test implementation cleaner.
        //    //DynamicMethod testMethod = new DynamicMethod(generatorMethod.Name, null, new Type[] { typeof(TestCaseInfo) }, generatorMethod.DeclaringType);
        //    //ILGenerator il = testMethod.GetILGenerator();

        //    //MethodInfo getCurrentMethod = typeof(MethodBase).GetMethod("GetCurrentMethod");
        //    //MethodInfo runTestMethod = typeof(Helper).GetMethod("RunTest");
        //    //il.EmitCall(OpCodes.Call, getCurrentMethod, null);
        //    //il.Emit(OpCodes.Ldarg_1);
        //    //il.EmitCall(OpCodes.Call, runTestMethod, null);
        //    //il.Emit(OpCodes.Ret);

        //    foreach (TestCaseInfo testCaseInfo in testCaseInfos)
        //    {
        //        TestCaseAttribute testCase = new TestCaseAttribute();
        //        testCase.Category = TestCategory.IDW;
        //        testCase.Owner = "dmetzgar";
        //        testCase.Author = "dmetzgar";
        //        testCase.Timeout = 300;
        //        addCase(testCase, testMethod, testCaseInfo.TestID);
        //    }
        //}

        public static void RunTest(MethodBase testMethod, TestCaseInfo testCaseInfo)
        {
            string source = GetMethodFullName(testMethod) + "#" + testCaseInfo.TestID;

            switch (testCaseInfo.TestDriver)
            {
                case TestDrivers.XamlSerializationDeserializationDoubleRoundtripDriver:
                    //XamlSerializationDeserializationDoubleRoundtripDriver.RunTest(source, testCaseInfo);
                    //break;
                case TestDrivers.XamlDeserializationSerializationDoubleRoundtripDriver:
                    //XamlDeserializationSerializationDoubleRoundtripDriver.RunTest(source, testCaseInfo);
                    //break;
                //case TestDrivers.DataContractSerializationDeserializationRoundtripDriver:
                //    if (testCaseInfo.RunTestDelegate == null)
                //    {
                //        throw new Exception("RunTestDelegate must be specified for this driver.");
                //    }
                //    testCaseInfo.RunTestDelegate(source, testCaseInfo);
                //    break;
                case TestDrivers.XamlReaderWriterDriver:
                    XamlReaderWriterDriver.RunTest(source, testCaseInfo);
                    break;
                default:
                    if (testCaseInfo.RunTestDelegate != null)
                    {
                        testCaseInfo.RunTestDelegate(source, testCaseInfo);
                        break;
                    }
                    else
                    {
                        throw new ArgumentException("Unknown driver");
                    }
            }
        }

        public static void TraceFile(MemoryStream stream, string outputFile)
        {
            using (StreamWriter outWriter = new StreamWriter(outputFile))
            {
                //Not closing the reader, otherwise the underlying memory stream will be closed.
                StreamReader reader = new StreamReader(stream);
                outWriter.Write(reader.ReadToEnd());
                outWriter.Flush();
            }
            stream.Position = 0;
        }

    }
}
