using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Xaml.Common;
using TestCases.Xaml.Common.XamlOM;
using TestCases.Xaml.Driver.XamlReaderWriter;

namespace TestCases.Xaml.Driver
{
    public class XamlReaderWriterDriver
    {
        public static void RunTest(string source, TestCaseInfo testCaseInfo)
        {
            IXamlReaderWriterTarget target = testCaseInfo.Target as IXamlReaderWriterTarget;
            bool pass;
            if (target == null)
            {
                throw new InvalidOperationException("Target must implement IXamlReaderWriterTarget");
            }

            try
            {
                Run(target, source);

                pass = false;
                pass ^= testCaseInfo.ExpectedResult;

                if (pass)
                {
                    Logger.Trace(source, "Pass");
                }
                else
                {
                    Logger.Trace(source, "Expected to fail, but passed instead.");
                }
            }
            catch (Exception e)
            {
                pass = true;
                pass ^= testCaseInfo.ExpectedResult;

                if (pass)
                {
                    if (!string.IsNullOrEmpty(testCaseInfo.ExpectedMessage))
                    {
                        if (testCaseInfo.ExpectedMessage != e.Message)
                        {
                            pass = false;
                        }
                    }
                    else
                    {
                        pass = false;
                    }

                    if (pass)
                    {
                        Logger.Trace(source, "Fail as expected. Failure detail:");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(testCaseInfo.ExpectedMessage))
                        {
                            Logger.Trace(source, "ExpectedMessage must be set when an exception is expected.");
                        }
                        else
                        {
                            Logger.Trace(source, "Expected {0}, but got following instead:", testCaseInfo.ExpectedMessage);
                        }
                    }
                }
                else
                {
                    Logger.Trace(source, "Expected to pass, but fail with following error:");
                }

                Logger.Trace(source, e.ToString());
            }

            if (!pass)
            {
                throw new DataTestException(string.Format("Test fails for '{0}'.", source));
            }
        }

        static void Run(IXamlReaderWriterTarget target, string source)
        {
            target.Document.ExpectedTrace.Trace.Steps.Clear();

            MemoryStream xaml = new MemoryStream();


            if (String.IsNullOrEmpty(target.Document.XamlString))
            {
                IXamlWriter writer = target.GetWriter(xaml);
                target.Document.Save(writer, true);
            }
            else
            {
                byte[] documentBytes = UnicodeEncoding.Unicode.GetBytes(target.Document.XamlString);
                xaml.Write(documentBytes, 0, documentBytes.Length);
            }

            if (Environment.GetEnvironmentVariable(Global.ToFileEnvironmentVariable) != null)
            {
                xaml.Position = 0;
                string rootPath = Path.Combine(/*Directory.GetCurrentDirectory()*/"\\", source);
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
                string outputFile = Path.Combine(rootPath, (Global.UniqueResultFileName + ".xml"));
                Helper.TraceFile(xaml, outputFile);
            }

            TraceXamlToLog(xaml);

            xaml.Position = 0;
            IXamlReader reader = target.GetReader(xaml);
            reader.TraceGuid = Guid.NewGuid();

            while (reader.Read()) { }

            if (String.IsNullOrEmpty(target.Document.XamlString))
            {
                TraceValidator.Validate(TestTraceManager.Instance.GetInstanceActualTrace(reader.TraceGuid), target.Document.ExpectedTrace);
            }
        }

        private static void TraceXamlToLog(MemoryStream stream)
        {

            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string stringValue = reader.ReadToEnd();
            stream.Position = 0;

            //Log.Info("Generated Xaml is : ");
            //Log.Info(stringValue);
        }

    }
}
