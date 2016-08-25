// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// experiment with serializing definition to json.net

using JsonFileInstanceStore;
using Newtonsoft.Json;
using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using System.IO;
using System.Reflection;
using System.Threading;
//using TestFileInstanceStore;

namespace BookmarkConsoleApp
{
    public static class Program
    {
        private static string s_fileInstanceStorePath;
        private static FileInstanceStore s_fileStore;
        private static AutoResetEvent s_idleEvent = new AutoResetEvent(false);
        private static AutoResetEvent s_completedEvent = new AutoResetEvent(false);
        private static AutoResetEvent s_unloadedEvent = new AutoResetEvent(false);

        public static void Main(string[] args)
        {
            string bookmarkData = "default bookmark data";
            Guid workflowInstanceId = Guid.Empty;
            bool createNewInstance = true;
            bool keepFiles = false;
            bool showUsage = false;

            Console.WriteLine("BookmarkConsoleApp");

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals("-storepath", args[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    s_fileInstanceStorePath = args[++i];
                    //Type[] knownTypes = new Type[] { typeof(Variable<int>.VariableLocation) };
                    //FileStore = new FileInstanceStore(FileInstanceStorePath, knownTypes);
                    s_fileStore = new FileInstanceStore(s_fileInstanceStorePath);
                    Console.WriteLine("Specified -storepath value = {0}", s_fileInstanceStorePath);
                }
                else if (string.Equals("-keepFiles", args[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    keepFiles = true;
                    Console.WriteLine("Specified -keepFiles");
                }
                else if (string.Equals("/?", args[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    showUsage = true;
                    break;
                }
                else
                {
                    Console.WriteLine("Unrecognized argument - {0}", args[i]);
                    showUsage = true;
                    break;
                }
            }

            if (showUsage || args.Length == 0)
            {
                PrintUsage();
                return;
            }

            if (s_fileStore != null)
            {
                s_fileStore.KeepInstanceDataAfterCompletion = keepFiles;
            }

            WorkflowApplication wfApp = CreateWorkflowApplication();

            if (s_fileStore != null)
            {
                Console.WriteLine("Create a new instance? (y/n, y is the default)");
                string createNewResponse = Console.ReadLine();
                if (string.Equals("n", createNewResponse, StringComparison.CurrentCultureIgnoreCase))
                {
                    createNewInstance = false;
                }

                if (!createNewInstance)
                {
                    Console.WriteLine("What instance id do you want to load?");
                    string instanceIdString = Console.ReadLine();
                    workflowInstanceId = new Guid(instanceIdString);
                }
            }

            if (createNewInstance)
            {
                workflowInstanceId = wfApp.Id;
                wfApp.Run();
                if (s_fileStore != null)
                {
                    s_unloadedEvent.WaitOne();
                }
                else
                {
                    s_idleEvent.WaitOne();
                }
            }

            string bookmarkName = workflowInstanceId.ToString();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("What string data should do you want to pass to the bookmark? (type stop to complete workflow)");
                bookmarkData = Console.ReadLine();

                if (s_fileStore != null)
                {
                    wfApp = CreateWorkflowApplication();
                    wfApp.Load(workflowInstanceId);
                }

                wfApp.ResumeBookmark(bookmarkName, bookmarkData);
                if (string.Compare(bookmarkData, "stop", true) == 0)
                {
                    s_completedEvent.WaitOne();
                    break;
                }
                if (s_fileStore != null)
                {
                    s_unloadedEvent.WaitOne();
                }
                else
                {
                    s_idleEvent.WaitOne();
                }
            }

            Console.WriteLine("Press <enter> to finish");
            Console.ReadLine();
        }

        public static WorkflowApplication CreateWorkflowApplication()
        {
            Activity wf = CreateWorkflow();
            WorkflowApplication result = new WorkflowApplication(wf);
            result.Idle = delegate (WorkflowApplicationIdleEventArgs e)
            {
                Console.WriteLine("Workflow idled");
                s_idleEvent.Set();
            };
            result.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Console.WriteLine("Workflow completed with state {0}.", e.CompletionState.ToString());
                if (e.TerminationException != null)
                {
                    Console.WriteLine("TerminationException = {0}; {1}", e.TerminationException.GetType().ToString(), e.TerminationException.Message);
                }
                s_completedEvent.Set();
            };
            result.Unloaded = delegate (WorkflowApplicationEventArgs e)
            {
                Console.WriteLine("Workflow unloaded");
                s_unloadedEvent.Set();
            };
            result.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs e)
            {
                if (s_fileStore != null)
                {
                    return PersistableIdleAction.Unload;
                }
                return PersistableIdleAction.None;
            };

            result.InstanceStore = s_fileStore;
            return result;
        }
        public static Activity CreateWorkflow()
        {
            Sequence workflow = new Sequence();
            workflow.Activities.Add(
                new WriteLine
                {
                    Text = "Before Bookmark"
                });
            workflow.Activities.Add(new BookmarkActivity());
            workflow.Activities.Add(
                new WriteLine
                {
                    Text = "After Bookmark"
                });

            // Let's serialize the definition to JSON.Net
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                // using TypeNameHandling.All to get all the types into the serialized data.
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                // NOT using ObjectCreationHandling.Replace so we can [de]serialize as Activity
            };
            try
            {
                var serializedDefinition = JsonConvert.SerializeObject(workflow, Formatting.Indented, jsonSerializerSettings);
                File.WriteAllText(Path.Combine(s_fileInstanceStorePath, "BookmarkConsoleApp-Definition.json"), serializedDefinition);
            }
            catch (Exception)
            {
                throw;
            }

            Activity deserializedActivity;
            try
            {
                var deserializedDefinition = File.ReadAllText(Path.Combine(s_fileInstanceStorePath, "BookmarkConsoleApp-Definition.json"));
                deserializedActivity = (Activity)JsonConvert.DeserializeObject(deserializedDefinition, jsonSerializerSettings);
            }
            catch (Exception)
            {
                throw;
            }

            return deserializedActivity;
        }

        private static void PrintUsage()
        {
            var exeName = typeof(Program).GetTypeInfo().Assembly.GetName().Name;
            Console.WriteLine("Usage : ");
            Console.WriteLine("\t{0} -storepath {{directory}} [-keepFiles]\n", exeName);
        }
    }
}
