using System;
using Xunit;

using System.Activities.XamlIntegration;
using System.Activities.Expressions;

using System.Diagnostics; // added
using System.IO;          // added
using System.Activities;  // Activity base
using System.Activities.Statements; // InvokeMethod, Sequence, Delay

#if NET6_0_OR_GREATER
using System.Activities; // CoreWF
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Activities.Tracing;
using Microsoft.CSharp.Activities;               // CSharpValue<T>, CSharpReference<T>
#endif

#if NET48
using System.Linq;
using Microsoft.CSharp.Activities;               // CSharpValue<T>, CSharpReference<T>
#endif

namespace MemoryTest
{
    public class BaseMemoryTest
    {
        public const int VariableAndArgumentCount = 1000;

        // ========= Dump control & paths =========
        private const bool CreateDumpBeforeDelay = true; // toggle to enable/disable
        private const string ProcDumpPath = @"C:\Tools\Procdump\procdump.exe";
        private const string DumpDirectory = @"C:\Users\bogdan.stan\Desktop\ROBO-5081 - OOM\CoreWF repro";
        private const string CreateDumpActivityName = "CreateDump (pre-delay)";
        private const string CreateGCActivityName = "GC";
        private const string DelayActivityName = "Delay";

        // Public entry point the workflow will invoke (via InvokeMethod)
        public static void CreateDumpBeforeDelayIfEnabled(string tag)
        {
            if (!CreateDumpBeforeDelay)
                return;

            TryCreateSelfDump(tag);
        }

        // Public entry point the workflow will invoke (via InvokeMethod)
        public static void RunGCIfDumpEnabled()
        {
            if (!CreateDumpBeforeDelay)
                return;

            //Run GC

            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();

        }

        // Pick the best ProcDump binary for the current process bitness
        private static string ResolveProcDumpExe()
        {
            try
            {
                var folder = Path.GetDirectoryName(ProcDumpPath) ?? "";
                if (Environment.Is64BitProcess)
                {
                    var p64 = Path.Combine(folder, "procdump64.exe");
                    if (File.Exists(p64))
                        return p64;
                }
            }
            catch { /* ignore, fall back */ }

            return ProcDumpPath; // default
        }

        // Fire-and-forget start via cmd.exe /c start /b "" "<exe>" <args>
        private static void StartDetachedViaCmd(string exe, string args, string? workingDir)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start /b \"\" \"{exe}\" {args}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDir) ? Environment.CurrentDirectory : workingDir
                };

                var p = Process.Start(psi);
                Debug.WriteLine($"[ProcDump] Fallback detach via cmd.exe started={p != null}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcDump] cmd.exe detach failed: {ex}");
            }
        }

        // Shared helper: actually run procdump (fire-and-forget on both frameworks)
        private static void TryCreateSelfDump(string tag)
        {
            try
            {
                Directory.CreateDirectory(DumpDirectory);

                int pid = Process.GetCurrentProcess().Id;
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dumpFile = Path.Combine(
                    DumpDirectory,
                    $"dump_{VariableAndArgumentCount}_{tag}_{ts}.dmp");

                var exe = ResolveProcDumpExe();
                var args = $"-accepteula -o -ma {pid} \"{dumpFile}\"";
                var workDir = Path.GetDirectoryName(exe);

                // Primary: start directly, do not wait
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = string.IsNullOrWhiteSpace(workDir) ? Environment.CurrentDirectory : workDir
                    };

                    var started = Process.Start(psi);
                    Debug.WriteLine($"[ProcDump] Direct start {(started != null ? "OK" : "FAILED")} | exe={exe} args={args}");

                    // If Process.Start returned null (very rare), try the detached cmd.exe route.
                    //if (started == null)
                    //{
                    //    StartDetachedViaCmd(exe, args, workDir);
                    //}
                }
                catch (Exception exDirect)
                {
                    Debug.WriteLine($"[ProcDump] Direct start failed, trying cmd.exe: {exDirect}");
                    throw;
                    //StartDetachedViaCmd(exe, args, workDir);
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: don't break the workflow/test if dump fails
                Debug.WriteLine($"[ProcDump] Dump creation failed: {ex}");
                throw;
            }
        }

        // Build the runtime activity that triggers the dump
        private static System.Activities.Activity BuildDumpActivity(string tag)
        {
            return new InvokeMethod
            {
                DisplayName = CreateDumpActivityName,
                TargetType = typeof(BaseMemoryTest),     // static call on this class
                MethodName = nameof(CreateDumpBeforeDelayIfEnabled),
                Parameters = { new InArgument<string>(tag) }
            };
        }  
        
        // Build the runtime activity that triggers the dump
        private static System.Activities.Activity BuildGCActivity()
        {
            return new InvokeMethod
            {
                DisplayName = CreateGCActivityName,
                TargetType = typeof(BaseMemoryTest),     // static call on this class
                MethodName = nameof(RunGCIfDumpEnabled)
            };
        }

#if NET6_0_OR_GREATER

        private ExpectedTrace ExpectedTraces;

        public void Validate(TestActivity wf)
        {
            TestRuntime.RunAndValidateWorkflow(wf, expectedTrace: ExpectedTraces);
        }

        public TestSequence AddFinalActivities(TestSequence seq, string testName)
        {
            var expectedTrace = seq.GetExpectedTrace();

            var lastStep = expectedTrace.Trace.Steps[expectedTrace.Trace.Steps.Count - 1];
            expectedTrace.Trace.Steps.Remove(lastStep); // remove final Completed step for now

            if (CreateDumpBeforeDelay)
            {
                // Extend expected trace to include our small InvokeMethod step
                expectedTrace.Trace.Steps.Add(new ActivityTrace(CreateGCActivityName, ActivityInstanceState.Executing));
                expectedTrace.Trace.Steps.Add(new ActivityTrace(CreateGCActivityName, ActivityInstanceState.Closed));

                expectedTrace.Trace.Steps.Add(new ActivityTrace(CreateDumpActivityName, ActivityInstanceState.Executing));
                expectedTrace.Trace.Steps.Add(new ActivityTrace(CreateDumpActivityName, ActivityInstanceState.Closed));

                // Insert the dump activity just before the first Delay in the product Sequence
                TryInsertDump(seq, testName);
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = DelayActivityName });
            expectedTrace.Trace.Steps.Add(new ActivityTrace(DelayActivityName, ActivityInstanceState.Executing));
            expectedTrace.Trace.Steps.Add(new ActivityTrace(DelayActivityName, ActivityInstanceState.Closed));

            expectedTrace.Trace.Steps.Add(lastStep); // re-add final Completed step

            ExpectedTraces = expectedTrace;
            return seq;
        }

        private static void TryInsertDump(object testSequence, string tag)
        {
            try
            {
                var productProp = testSequence.GetType().GetProperty("ProductActivity");
                if (productProp?.GetValue(testSequence) is Sequence productSeq)
                {
                    productSeq.Activities.Add(BuildGCActivity());
                    productSeq.Activities.Add(BuildDumpActivity(tag));
                }
                else
                {
                    Debug.WriteLine("[ProcDump] Could not access underlying Sequence to inject dump activity.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcDump] Injection failed: {ex}");
            }
        }

#elif NET48

        public void Validate(Activity<string> wf)
        {
            // Invoke and assert the composed string.
            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", System.Linq.Enumerable.Range(0, VariableAndArgumentCount).Select(i => $"I'm variable {i}"));

            Assert.Equal(expected, actual);
        }

        public Sequence AddFinalActivities(Sequence seq, string testName)
        {
            if (CreateDumpBeforeDelay)
            {
                seq.Activities.Add(BuildDumpActivity(testName));
            }

            // If you also want to explicitly add a Delay here, uncomment below:
            seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

            return seq;
        }
#endif
    }
}
