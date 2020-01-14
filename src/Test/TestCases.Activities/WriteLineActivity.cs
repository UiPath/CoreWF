// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities
{
    public class WriteLineActivity : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly string _tempFile1Path;
        private readonly TextWriter _origConsoleOut;

        public WriteLineActivity()
        {
            _tempFilePath = Path.GetTempFileName();
            _tempFile1Path = Path.GetTempFileName();
            _origConsoleOut = Console.Out;
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
            if (File.Exists(_tempFile1Path))
                File.Delete(_tempFile1Path);
            Console.SetOut(_origConsoleOut);
        }

        /// <summary>
        /// Verify Writeline activity can write on console
        /// </summary>        
        [Fact]
        public void VerifyWriteTextOnConsole()
        {
            string stringToWrite = "Hello World";

            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                {
                    TestProductWriteline writeline = new TestProductWriteline("Write Hello")
                    {
                        Text = stringToWrite
                    };

                    TestRuntime.RunAndValidateWorkflow(writeline);
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Verify WriteLine activity can write special characters on console
        /// </summary>        
        [Fact]
        public void WriteSpecialCharsOnConsole()
        {
            string stringToWrite = "@ ;=#$%^&*()_+!";

            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                TestProductWriteline writeline = new TestProductWriteline("Write Special Characters")
                {
                    Text = stringToWrite
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Verify activity can take Text as null and it won't throw exception
        /// </summary>        
        [Fact]
        public void WriteNullOnConsole()
        {
            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                {
                    TestProductWriteline writeline = new TestProductWriteline("Write Null on Console")

                    {
                        Text = null
                    };

                    TestRuntime.RunAndValidateWorkflow(writeline);
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, String.Empty);
        }

        /// <summary>
        /// Verify activity can take null as TextWriter
        /// Verify activity can take null as TextWriterand will not throw exception and write to console by default
        /// </summary>        
        [Fact]
        public void VerifyNullAsTextWriter()
        {
            string stringToVerify = "Null Text Writer";

            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                TestProductWriteline writeline = new TestProductWriteline("Null Text Writer")
                {
                    Text = stringToVerify,
                    TextWriterExpression = context => null
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToVerify);
        }

        /// <summary>
        /// Verify activity can take null text and null text writer and  will not throw exception
        /// </summary>        
        [Fact]
        public void VerifyNullTextAndNullTextWriter()
        {
            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                TestProductWriteline writeline = new TestProductWriteline("Null Text and Null TextWriter")
                {
                    Text = null,
                    TextWriterExpression = context => null
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, string.Empty);
        }

        /// <summary>
        /// Verify activity can write to a file using StreamWriter
        /// </summary>        
        [Fact]
        public void WriteInTextFileWithStreamWriter()
        {
            string stringToWrite = "Writing text to  a file with StreamWriter object";

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
            {
                Dictionary<string, object> inputs = new Dictionary<string, object>();
                inputs.Add("Text", stringToWrite);
                inputs.Add("TextWriter", streamWriter);
                TestProductWriteline writeline = new TestProductWriteline("Write with StreamWriter");

                TestRuntime.RunAndValidateUsingWorkflowInvoker(writeline, inputs, null, null);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Verify we can use WriteLine activity to write to a file and then again
        /// Verify we can use WriteLine activity to write to a file and then againa new WriteLine to write to same file. Verify WriteLine activity do not overwrite the file content.
        /// </summary>        
        [Fact]
        public void WriteSameFileUseWritelineInSequence()
        {
            string stringToWrite1 = "Writing by first writeLine";
            string stringToWrite2 = "Writing by second writeline";

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
            {
                TestSequence sequence = new TestSequence
                {
                    Activities =
                {
                    new TestProductWriteline("First WriteLine Activity")
                    {
                        Text = stringToWrite1,
                        TextWriterExpression = context => streamWriter
                    },

                    new TestProductWriteline("Second WriteLine Activity")
                    {
                        Text = stringToWrite2,
                        TextWriterExpression = context => streamWriter
                    }
                }
                };

                TestRuntime.RunAndValidateWorkflow(sequence);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite1, stringToWrite2);
        }

        /// <summary>
        /// Verify activity can take text as input which is output of reading a file
        /// </summary>        
        [Fact]
        public void WriteFileByReadingOther()
        {
            string stringToWrite = "Writing to this file for a test case, so we can read from this file";

            using (StreamWriter writer = new StreamWriter(new FileStream(_tempFile1Path, FileMode.Create, FileAccess.Write)))
            {
                using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
                {
                    streamWriter.WriteLine(stringToWrite);
                }

                TestProductWriteline writeline = new TestProductWriteline("Write with StreamWriter")
                {
                    Text = File.ReadAllLines(_tempFilePath)[0],
                    TextWriterExpression = context => writer
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFile1Path, stringToWrite);
        }

        /// <summary>
        /// Verify Console can be passed explicitly as a TextWriter to activity
        /// </summary>        
        [Fact]
        public void WriteTextExplicitPassConsole()
        {
            string stringToWrite = "@ ;=#$%^&*()_+!    ";

            using (StreamWriter writer = ConsoleSetOut())
            {
                TestProductWriteline writeline = new TestProductWriteline("Write Special Characters")
                {
                    Text = stringToWrite,
                    TextWriterExpression = context => Console.Out
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Verify WriteLine activity can write newline characters to console
        /// </summary>        
        [Fact]
        public void WriteNewLineCharsToConsole()
        {
            string stringToWrite = "Take it easy";

            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                TestProductWriteline writeline = new TestProductWriteline("Write Special Characters")
                {
                    Text = "\n\n" + stringToWrite
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, string.Empty, string.Empty, stringToWrite);
        }

        /// <summary>
        /// Verify activity can write text using StringWriter
        /// </summary>        
        [Fact]
        public void WriteTextUsingStringWriter()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string stringToWrite = "\nWriting text to  a file with StreamWriter object";

            using (StringWriter stringWriter = new StringWriter(stringBuilder))
            {
                TestSequence sequence = new TestSequence
                {
                    Activities =
                        {
                            new TestProductWriteline("Write with StringWriter")
                            {
                                Text = stringToWrite,
                                TextWriterExpression = context => stringWriter
                            }
                        }
                };

                TestRuntime.RunAndValidateWorkflow(sequence);

                Assert.True((stringBuilder.ToString().Equals(stringToWrite + Environment.NewLine)), "String builder did not equal the string to be written.");
            }
        }

        /// <summary>
        /// Custom text writer used to throw exceptions
        /// </summary>        
        [Fact]
        public void CustomTextWriterToWriteLineActivity()
        {
            TestSequence sequence = new TestSequence
            {
                Activities =
                {
                    new TestProductWriteline("ArgumentOutOfRangeException")
                    {
                        Text = "HELLO",
                        TextWriterExpression = context => new CustomTextWriter(new ArgumentOutOfRangeException()),
                        ExpectedOutcome = Outcome.UncaughtException( typeof(ArgumentOutOfRangeException))
                    }
                }
            };

            TestRuntime.RunAndValidateAbortedException(
                sequence,
                typeof(ArgumentOutOfRangeException),
                new Dictionary<string, string>());
        }

        /// <summary>
        /// WriteLine in while activity
        /// </summary>        
        [Fact]
        public void WriteLineInWhileActivity()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestAssign<int> increment = new TestAssign<int>("Increment Counter");

            string textVariable = "Loop";

            Variable<string> stringToWrite = VariableHelper.CreateInitialized<string>(
                "stringToWrite",
                textVariable);

            // We don't really need to do Console.SetOut for this test. But we use
            // ConsoleSetOut to create the streamWriter that we use as the TextWriter
            // property of the product's WriteLine activity.
            using (StreamWriter streamWriter = ConsoleSetOut(false))
            {
                Variable<TextWriter> writer = VariableHelper.CreateInitialized<TextWriter>(
                "writer",
                streamWriter);

                increment.ToVariable = counter;
                increment.ValueExpression = ((env) => (((int)counter.Get(env))) + 1);

                TestSequence innersequence = new TestSequence
                {
                    Variables =
                    {
                        stringToWrite,
                        writer
                    },

                    Activities =
                    {
                        new TestProductWriteline("Write on a file in while loop")
                        {
                            TextExpression = ((env) => (((string) stringToWrite.Get(env))) + counter.Get(env).ToString()),
                            TextWriterVariable = writer,
                        },

                       increment,
                    }
                };

                TestSequence outerSequence = new TestSequence
                {
                    Activities =
                    {
                        new TestWhile("While loop")
                        {
                            Body = innersequence,
                            ConditionExpression = ((env) => ((int)counter.Get(env)) < 5),
                            HintIterationCount = 5
                        }
},

                    Variables =
                    {
                        counter
                    }
                };

                TestRuntime.RunAndValidateWorkflow(outerSequence);
            }

            VerifyTextOfWriteLine(
                _tempFilePath,
                textVariable + "0",
                textVariable + "1",
                textVariable + "2",
                textVariable + "3",
                textVariable + "4");
        }

        /// <summary>
        /// WriteChineseBig5CharsToFile
        /// Write Chinese Big5 characters to a file.
        /// </summary>        
        [Fact]
        public void WriteChineseBig5CharsToFile()
        {
            string stringToWrite = "\u507D \u505C \u5047 \u5043 \u504C \u505A \u5049 \u5065 \u5076 \u504E \u5055 \u5075 \u5074 ";

            using (StreamWriter sw = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write), Encoding.Unicode))
            {
                TestProductWriteline writeline = new TestProductWriteline("Write Chinese Big5 chars to file")
                {
                    Text = stringToWrite,
                    TextWriterExpression = context => sw
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// WriteChineseGBToConsole
        /// Write Chinese GB characters to console.
        /// </summary>        
        [Fact]
        public void WriteChineseGBToConsole()
        {
            string stringToWrite = "\u00E9\u00A6\u02C6 \u00E6\u201E\u00A7 \u00E6\u00BA\u0192 \u00E5\u009D\u00A4 \u00E6\u02DC\u2020 \u00E6\u008D\u2020 \u00E5\u203A\u00B0 \u00E6\u2039\u00AC \u00E6\u2030\u00A9 \u00E5\u00BB\u201C \u00E9\u02DC\u201D \u00E5\u017E\u0192 \u00E6\u2039\u2030 \u00E5\u2013\u2021 \u00E8\u0153\u00A1 ";

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write), Encoding.Unicode))
            {
                Console.SetOut(streamWriter);
                streamWriter.AutoFlush = true;

                TestProductWriteline writeline = new TestProductWriteline("Write Chinese Big5 chars to file")
                {
                    Text = stringToWrite
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// WriteChineseAndEnglishConsole
        /// </summary>        
        [Fact]
        public void WriteChineseAndEnglishConsole()
        {
            string stringToWrite = "\u9D1B\u9ED8\u9ED4\u9F8D\u9F9C\u512A\u511F\u5121 \u52F5 \u568E \u5680 \u5690 \u5685 \u5687 hello how are you";

            TestProductWriteline writeline = new TestProductWriteline("Write Chinese Big5 chars to file")
            {
                Text = stringToWrite
            };

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write), Encoding.Unicode))
            {
                Console.SetOut(streamWriter);


                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// WriteArabicText
        /// </summary>        
        [Fact]
        public void WriteArabicText()
        {
            string stringToWrite = "\u062A\u0639\u0644\u0645 \u0627\u0644\u0644\u063A\u0629 \u0627\u0644\u0639\u0631\u0628\u064A\u0629 \u062A\u0639\u0644\u0645 \u0627\u0644\u0644\u063A\u0629 \u0627\u0644\u0639\u0631\u0628\u064A\u0629";

            using (StreamWriter sw = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write), Encoding.Unicode))
            {
                TestProductWriteline writeline = new TestProductWriteline("Write Chinese Big5 chars to file")
                {
                    Text = stringToWrite,
                    TextWriterExpression = context => sw
                };

                TestRuntime.RunAndValidateWorkflow(writeline);
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// WriteLine activity will be executed and closed and then workflow will be persisted
        /// </summary>        
        [Fact]
        public void PersistenceToWriteLineActivity()
        {
            TestProductWriteline writeLine1 = new TestProductWriteline("writeLine1")
            {
                TextExpressionActivity = new TestExpressionEvaluatorWithBody<string>()
                {
                    Body = (new TestBlockingActivity("BlockingActivity")),
                    ExpressionResult = "This should be displayed after the bookmark"
                }
            };

            using (StreamWriter writer = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
            {
                Console.SetOut(writer);

                TestBlockingActivity blocking = new TestBlockingActivity("BlockingActivity");

                JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

                using (TestWorkflowRuntime workflow = TestRuntime.CreateTestWorkflowRuntime(writeLine1, null, jsonStore, PersistableIdleAction.None))
                {
                    workflow.ExecuteWorkflow();
                    workflow.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                    workflow.PersistWorkflow();
                    workflow.ResumeBookMark("BlockingActivity", null);
                    workflow.WaitForCompletion();
                }
            }
        }

        /// <summary>
        /// WirteLine with WorkflowInvoker
        /// </summary>        
        [Fact]
        public void WriteLineWithWorkflowInvoker()
        {
            string stringToWrite = "Hello World";
            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                {
                    TestProductWriteline writeline = new TestProductWriteline("Write Hello");
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic.Add("Text", stringToWrite);
                    TestRuntime.RunAndValidateUsingWorkflowInvoker(writeline, dic, null, null);
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Add a TextWriter extension to the extensions and verify that WriteLine activity writes to that TextWriter.
        /// </summary>        
        [Fact]
        public void AddTextWriterExtensionToRuntimeAndUseWriteLine()
        {
            string stringToWrite = "Writing text to  a file with StreamWriter object";

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
            {
                TestProductWriteline writeline = new TestProductWriteline("Write with StreamWriter")
                {
                    Text = stringToWrite,
                };

                using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(writeline))
                {
                    runtime.CreateWorkflow();
                    runtime.Extensions.Add(streamWriter);
                    runtime.ResumeWorkflow();
                    runtime.WaitForCompletion();
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }


        /// <summary>
        /// Add a TextWriter extension and set the WriteLine.TextWriter property. Make sure the WriteLine.TextWriter is used.
        /// </summary>        
        [Fact]
        public void AddTextWriterExtensionToRuntimeAndUseWriteLineWithTextWriter()
        {
            string stringToWrite = "Writing text to  a file with StreamWriter object";

            using (StreamWriter streamWriter1 = new StreamWriter(new MemoryStream()))
            {
                using (StreamWriter streamWriter2 = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
                {
                    TestProductWriteline writeline = new TestProductWriteline("Write with StreamWriter")
                    {
                        Text = stringToWrite,
                        TextWriterExpression = context => streamWriter2,
                    };

                    using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(writeline))
                    {
                        runtime.CreateWorkflow();
                        runtime.Extensions.Add(streamWriter1);
                        runtime.ResumeWorkflow();
                        runtime.WaitForCompletion();
                    }
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// Add a TextWriterExtension and set the TextWriter to null. Verify that text is being written to TextWriter extention.
        /// </summary>        
        [Fact]
        public void SetTextWriterToNullAndUseATextWriterExtension()
        {
            string stringToWrite = "Writing text to  a file with StreamWriter object";

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write)))
            {
                TestProductWriteline writeline = new TestProductWriteline("Write with StreamWriter")
                {
                    Text = stringToWrite,
                };
                writeline.ProductWriteLine.TextWriter = null;

                using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(writeline))
                {
                    runtime.CreateWorkflow();
                    runtime.Extensions.Add(streamWriter);
                    runtime.ResumeWorkflow();
                    runtime.WaitForCompletion();
                }
            }

            VerifyTextOfWriteLine(_tempFilePath, stringToWrite);
        }

        /// <summary>
        /// This will verify the text written by writeLine activity 
        /// and the text we passed are same or not. If not same this method will throw 
        /// exception.
        /// </summary>
        /// <param name="path">Path of the file where WriteLine activity has written</param>
        /// <param name="textToVerify">Texts which we are expecting to present in the file</param>
        private static void VerifyTextOfWriteLine(string path, params string[] textToVerify)
        {
            if (textToVerify == null)
            {
                throw new Exception("Input string cannot be null to verify");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            string[] texts = File.ReadAllLines(path);

            if (!(texts.Length == textToVerify.Length))
            {
                throw new Exception(string.Format("Expecting {0} strings to verify and actually got {1}", textToVerify.Length, texts.Length));
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (!texts[i].Equals(textToVerify[i]))
                {
                    throw new Exception(string.Format("Expecting '{0}' to verify and actually got '{1}'", textToVerify[i], texts[i]));
                }
            }
        }

        /// <summary>
        /// This will redirect console output to the file
        /// given to StreamWriter.
        /// </summary>
        /// <returns>StreamWriter where we redirect the console output</returns>
        private StreamWriter ConsoleSetOut(bool doConsoleSetOut = true)
        {
            StreamWriter streamWriter = new StreamWriter(new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write))
            {
                AutoFlush = true
            };

            if (doConsoleSetOut)
            {
                Console.SetOut(streamWriter);
            }

            return streamWriter;
        }

        /// <summary>
        /// Text writer we are using to throw exceptions
        /// </summary>
        public class CustomTextWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            private readonly Exception _exception;

            public CustomTextWriter(Exception exception)
            {
                _exception = exception;
            }

            public override void WriteLine(string value)
            {
                if (_exception != null)
                    throw _exception;
            }

            public override void Write(char value)
            {
                //None
            }
        }
    }
}
