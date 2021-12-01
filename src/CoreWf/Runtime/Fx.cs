// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace System.Activities.Runtime;

internal static class Fx
{
    private const string defaultEventSource = "System.Runtime";

#if DEBUG
    private const string BreakOnExceptionTypesName = "BreakOnExceptionTypes";

    private static bool s_breakOnExceptionTypesRetrieved;
    private static Type[] s_breakOnExceptionTypesCache;
#endif

    private static ExceptionTrace s_exceptionTrace;

    [Tag.SecurityNote(Critical = "This delegate is called from within a ConstrainedExecutionRegion, must not be settable from PT code")]
    [SecurityCritical]
    private static ExceptionHandler s_asynchronousThreadExceptionHandler;

    public static ExceptionTrace Exception
    {
        get
        {
            // don't need a lock here since a true singleton is not required
            s_exceptionTrace ??= new ExceptionTrace(defaultEventSource/*, Trace*/);
            return s_exceptionTrace;
        }
    }

    public static bool IsEtwProviderEnabled => WfEventSource.Instance.IsEnabled();

    public static ExceptionHandler AsynchronousThreadExceptionHandler
    {
        [Tag.SecurityNote(Critical = "access critical field", Safe = "ok for get-only access")]
        [SecuritySafeCritical]
        get => s_asynchronousThreadExceptionHandler;

        [Tag.SecurityNote(Critical = "sets a critical field")]
        [SecurityCritical]
        set => s_asynchronousThreadExceptionHandler = value;
    }

    // Do not call the parameter "message" or else FxCop thinks it should be localized.
    [Conditional("DEBUG")]
    public static void Assert(bool condition, string description)
    {
        if (!condition)
        {
            Assert(description);
        }
    }

    [Conditional("DEBUG")]
    public static void Assert(string description)
    {
        //AssertHelper.FireAssert(description);
    }

    public static void AssertAndThrow(bool condition, string description)
    {
        if (!condition)
        {
            AssertAndThrow(description);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception AssertAndThrow(string description)
    {
        Assert(description);
        if (WfEventSource.Instance.ShipAssertExceptionMessageIsEnabled())
            WfEventSource.Instance.ShipAssertExceptionMessage(description);
        //TraceCore.ShipAssertExceptionMessage(Trace, description);
        throw new InternalException(description);
    }

    public static void AssertAndThrowFatal(bool condition, string description)
    {
        if (!condition)
        {
            AssertAndThrowFatal(description);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception AssertAndThrowFatal(string description)
    {
        Assert(description);
        if (WfEventSource.Instance.ShipAssertExceptionMessageIsEnabled())
            WfEventSource.Instance.ShipAssertExceptionMessage(description);
        //TraceCore.ShipAssertExceptionMessage(Trace, description);
        throw new FatalInternalException(description);
    }

    public static void AssertAndFailFast(bool condition, string description)
    {
        if (!condition)
        {
            AssertAndFailFast(description);
        }
    }

    // This never returns.  The Exception return type lets you write 'throw AssertAndFailFast()' which tells the compiler/tools that
    // execution stops.
    [Tag.SecurityNote(Critical = "Calls into critical method Environment.FailFast",
        Safe = "The side affect of the app crashing is actually intended here")]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception AssertAndFailFast(string description)
    {
        Assert(description);
        string failFastMessage = SR.FailFastMessage(description);

        // The catch is here to force the finally to run, as finallys don't run until the stack walk gets to a catch.  
        // The catch makes sure that the finally will run before the stack-walk leaves the frame, but the code inside is impossible to reach.
        try
        {
            try
            {
                Exception.TraceFailFast(failFastMessage);
            }
            finally
            {
                Environment.FailFast(failFastMessage);
            }
        }
        catch
        {
            throw;
        }

        return null; // we'll never get here since we've just fail-fasted
    }

    public static bool IsFatal(Exception exception)
    {
        while (exception != null)
        {
            if (exception is FatalException ||
                exception is OutOfMemoryException ||
                exception is FatalInternalException)
            {
                return true;
            }

            // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
            // we want to check to see whether they've been used to wrap a fatal exception.  If so, then they
            // count as fatal.
            if (exception is TypeInitializationException ||
                exception is TargetInvocationException)
            {
                exception = exception.InnerException;
            }
            else if (exception is AggregateException aggregateException)
            {
                // AggregateExceptions have a collection of inner exceptions, which may themselves be other
                // wrapping exceptions (including nested AggregateExceptions).  Recursively walk this
                // hierarchy.  The (singular) InnerException is included in the collection.
                ReadOnlyCollection<Exception> innerExceptions = aggregateException.InnerExceptions;
                foreach (Exception innerException in innerExceptions)
                {
                    if (IsFatal(innerException))
                    {
                        return true;
                    }
                }

                break;
            }
            else
            {
                break;
            }
        }

        return false;
    }

    // This method should be only used for debug build.
    public static bool AssertsFailFast => false;

    // This property should be only used for debug build.
    public static Type[] BreakOnExceptionTypes
    {
        get
        {
#if DEBUG
            if (!s_breakOnExceptionTypesRetrieved)
            {
                if (TryGetDebugSwitch(BreakOnExceptionTypesName, out object value))
                {
                    if (value is string[] typeNames && typeNames.Length > 0)
                    {
                        s_breakOnExceptionTypesCache = typeNames.Select(t => Type.GetType(t, false)).ToArray();
                    }
                }
                s_breakOnExceptionTypesRetrieved = true;
            }
            return s_breakOnExceptionTypesCache;
#else
            return null;
#endif
        }
    }

    // This property should be only used for debug build.
    public static bool StealthDebugger => false;

#if DEBUG
#pragma warning disable IDE0060 // Remove unused parameter
    private static bool TryGetDebugSwitch(string name, out object value)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        value = null;
        return false;
    }
#endif

    public static AsyncCallback ThunkCallback(AsyncCallback callback) => new AsyncThunk(callback).ThunkFrame;

    public static SendOrPostCallback ThunkCallback(SendOrPostCallback callback) => new SendOrPostThunk(callback).ThunkFrame;

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes,
    //    Justification = "Don't want to hide the exception which is about to crash the process.")]
    [Tag.SecurityNote(Miscellaneous = "Must not call into PT code as it is called within a CER.")]
    private static void TraceExceptionNoThrow(Exception exception)
    {
        try
        {
            // This call exits the CER.  However, when still inside a catch, normal ThreadAbort is prevented.
            // Rude ThreadAbort will still be allowed to terminate processing.
            Exception.TraceUnhandledException(exception);
        }
        catch
        {
            // This empty catch is only acceptable because we are a) in a CER and b) processing an exception
            // which is about to crash the process anyway.
        }
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes,
    //    Justification = "Don't want to hide the exception which is about to crash the process.")]
    //[SuppressMessage(FxCop.Category.ReliabilityBasic, FxCop.Rule.IsFatalRule,
    //    Justification = "Don't want to hide the exception which is about to crash the process.")]
    [Tag.SecurityNote(Miscellaneous = "Must not call into PT code as it is called within a CER.")]
    private static bool HandleAtThreadBase(Exception exception)
    {
        // This area is too sensitive to do anything but return.
        if (exception == null)
        {
            Assert("Null exception in HandleAtThreadBase.");
            return false;
        }

        TraceExceptionNoThrow(exception);

        try
        {
            ExceptionHandler handler = AsynchronousThreadExceptionHandler;
            return handler != null && handler.HandleException(exception);
        }
        catch (Exception secondException)
        {
            // Don't let a new exception hide the original exception.
            TraceExceptionNoThrow(secondException);
        }

        return false;
    }

    //private static void UpdateLevel(EtwDiagnosticTrace trace)
    //{
    //    if (trace == null)
    //    {
    //        return;
    //    }

    //    if (TraceCore.ActionItemCallbackInvokedIsEnabled(trace) ||
    //        TraceCore.ActionItemScheduledIsEnabled(trace))
    //    {
    //        trace.SetEnd2EndActivityTracingEnabled(true);
    //    }
    //}

    //private static void UpdateLevel()
    //{
    //    UpdateLevel(Fx.Trace);
    //}

    public abstract class ExceptionHandler
    {
        [Tag.SecurityNote(Miscellaneous = "Must not call into PT code as it is called within a CER.")]
        public abstract bool HandleException(Exception exception);
    }

    public static class Tag
    {
        public enum CacheAttrition
        {
            None,
            ElementOnTimer,

            // A finalizer/WeakReference based cache, where the elements are held by WeakReferences (or hold an
            // inner object by a WeakReference), and the weakly-referenced object has a finalizer which cleans the
            // item from the cache.
            ElementOnGC,

            // A cache that provides a per-element token, delegate, interface, or other piece of context that can
            // be used to remove the element (such as IDisposable).
            ElementOnCallback,

            FullPurgeOnTimer,
            FullPurgeOnEachAccess,
            PartialPurgeOnTimer,
            PartialPurgeOnEachAccess,
        }

        public enum ThrottleAction
        {
            Reject,
            Pause,
        }

        public enum ThrottleMetric
        {
            Count,
            Rate,
            Other,
        }

        public enum Location
        {
            InProcess,
            OutOfProcess,
            LocalSystem,
            LocalOrRemoteSystem, // as in a file that might live on a share
            RemoteSystem,
        }

        public enum SynchronizationKind
        {
            LockStatement,
            MonitorWait,
            MonitorExplicit,
            InterlockedNoSpin,
            InterlockedWithSpin,

            // Same as LockStatement if the field type is object.
            FromFieldType,
        }

        [Flags]
        public enum BlocksUsing
        {
            MonitorEnter,
            MonitorWait,
            ManualResetEvent,
            AutoResetEvent,
            AsyncResult,
            IAsyncResult,
            PInvoke,
            InputQueue,
            ThreadNeutralSemaphore,
            PrivatePrimitive,
            OtherInternalPrimitive,
            OtherFrameworkPrimitive,
            OtherInterop,
            Other,

            NonBlocking, // For use by non-blocking SynchronizationPrimitives such as IOThreadScheduler
        }

        public static class Strings
        {
            internal const string ExternallyManaged = "externally managed";
            internal const string AppDomain = "AppDomain";
            internal const string DeclaringInstance = "instance of declaring class";
            internal const string Unbounded = "unbounded";
            internal const string Infinite = "infinite";
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Class,
            AllowMultiple = true, Inherited = false)]
        [Conditional("DEBUG")]
        public sealed class FriendAccessAllowedAttribute : Attribute
        {
            public FriendAccessAllowedAttribute(string assemblyName) :
                base()
            {
                AssemblyName = assemblyName;
            }

            public string AssemblyName { get; set; }
        }

        public static class Throws
        {
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor,
                AllowMultiple = true, Inherited = false)]
            [Conditional("CODE_ANALYSIS_CDF")]
            public sealed class TimeoutAttribute : ThrowsAttribute
            {
                public TimeoutAttribute() :
                    this("The operation timed out.") { }

                public TimeoutAttribute(string diagnosis) :
                    base(typeof(TimeoutException), diagnosis) { }
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class CacheAttribute : Attribute
        {
            private readonly Type _elementType;
            private readonly CacheAttrition _cacheAttrition;

            public CacheAttribute(Type elementType, CacheAttrition cacheAttrition)
            {
                Scope = Strings.DeclaringInstance;
                SizeLimit = Strings.Unbounded;
                Timeout = Strings.Infinite;
                _elementType = elementType ?? throw Exception.ArgumentNull(nameof(elementType));
                _cacheAttrition = cacheAttrition;
            }

            public Type ElementType => _elementType;

            public CacheAttrition CacheAttrition => _cacheAttrition;

            public string Scope { get; set; }
            public string SizeLimit { get; set; }
            public string Timeout { get; set; }
        }

        [AttributeUsage(AttributeTargets.Field)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class QueueAttribute : Attribute
        {
            private readonly Type _elementType;

            public QueueAttribute(Type elementType)
            {
                Scope = Strings.DeclaringInstance;
                SizeLimit = Strings.Unbounded;
                _elementType = elementType ?? throw Exception.ArgumentNull(nameof(elementType));
            }

            public Type ElementType => _elementType;

            public string Scope { get; set; }
            public string SizeLimit { get; set; }
            public bool StaleElementsRemovedImmediately { get; set; }
            public bool EnqueueThrowsIfFull { get; set; }
        }

        [AttributeUsage(AttributeTargets.Field)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class ThrottleAttribute : Attribute
        {
            private readonly ThrottleAction _throttleAction;
            private readonly ThrottleMetric _throttleMetric;
            private readonly string _limit;

            public ThrottleAttribute(ThrottleAction throttleAction, ThrottleMetric throttleMetric, string limit)
            {
                Scope = Strings.AppDomain;

                if (string.IsNullOrEmpty(limit))
                {
                    throw Exception.ArgumentNullOrEmpty(nameof(limit));
                }

                _throttleAction = throttleAction;
                _throttleMetric = throttleMetric;
                _limit = limit;
            }

            public ThrottleAction ThrottleAction => _throttleAction;

            public ThrottleMetric ThrottleMetric => _throttleMetric;

            public string Limit => _limit;

            public string Scope { get; set; }
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor,
            AllowMultiple = true, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class ExternalResourceAttribute : Attribute
        {
            private readonly Location _location;
            private readonly string _description;

            public ExternalResourceAttribute(Location location, string description)
            {
                _location = location;
                _description = description;
            }

            public Location Location => _location;

            public string Description => _description;
        }

        // Set on a class when that class uses lock (this) - acts as though it were on a field
        //     private object this;
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class SynchronizationObjectAttribute : Attribute
        {
            public SynchronizationObjectAttribute()
            {
                Blocking = true;
                Scope = Strings.DeclaringInstance;
                Kind = SynchronizationKind.FromFieldType;
            }

            public bool Blocking { get; set; }
            public string Scope { get; set; }
            public SynchronizationKind Kind { get; set; }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class SynchronizationPrimitiveAttribute : Attribute
        {
            private readonly BlocksUsing _blocksUsing;

            public SynchronizationPrimitiveAttribute(BlocksUsing blocksUsing)
            {
                _blocksUsing = blocksUsing;
            }

            public BlocksUsing BlocksUsing => _blocksUsing;

            public bool SupportsAsync { get; set; }
            public bool Spins { get; set; }
            public string ReleaseMethod { get; set; }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class BlockingAttribute : Attribute
        {
            public BlockingAttribute() { }

            public string CancelMethod { get; set; }
            public Type CancelDeclaringType { get; set; }
            public string Conditional { get; set; }
        }

        // Sometime a method will call a conditionally-blocking method in such a way that it is guaranteed
        // not to block (i.e. the condition can be Asserted false).  Such a method can be marked as
        // GuaranteeNonBlocking as an assertion that the method doesn't block despite calling a blocking method.
        //
        // Methods that don't call blocking methods and aren't marked as Blocking are assumed not to block, so
        // they do not require this attribute.
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class GuaranteeNonBlockingAttribute : Attribute
        {
            public GuaranteeNonBlockingAttribute() { }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class NonThrowingAttribute : Attribute
        {
            public NonThrowingAttribute() { }
        }

        //[SuppressMessage(FxCop.Category.Performance, "CA1813:AvoidUnsealedAttributes",
        //    Justification = "This is intended to be an attribute heirarchy. It does not affect product perf.")]
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor,
            AllowMultiple = true, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public class ThrowsAttribute : Attribute
        {
            private readonly Type _exceptionType;
            private readonly string _diagnosis;

            public ThrowsAttribute(Type exceptionType, string diagnosis)
            {
                if (string.IsNullOrEmpty(diagnosis))
                {
                    throw Exception.ArgumentNullOrEmpty(nameof(diagnosis));
                }

                _exceptionType = exceptionType ?? throw Exception.ArgumentNull(nameof(exceptionType));
                _diagnosis = diagnosis;
            }

            public Type ExceptionType => _exceptionType;

            public string Diagnosis => _diagnosis;
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class InheritThrowsAttribute : Attribute
        {
            public InheritThrowsAttribute() { }

            public Type FromDeclaringType { get; set; }
            public string From { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class KnownXamlExternalAttribute : Attribute
        {
            public KnownXamlExternalAttribute() { }
        }

        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class XamlVisibleAttribute : Attribute
        {
            public XamlVisibleAttribute()
                : this(true) { }

            public XamlVisibleAttribute(bool visible)
            {
                Visible = visible;
            }

            public bool Visible { get; private set; }
        }

        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class |
            AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface |
            AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
        [Conditional("CODE_ANALYSIS_CDF")]
        public sealed class SecurityNoteAttribute : Attribute
        {
            public SecurityNoteAttribute() { }

            public string Critical { get; set; }

            public string Safe { get; set; }

            public string Miscellaneous { get; set; }
        }
    }

    internal abstract class Thunk<T> where T : class
    {
        [Tag.SecurityNote(Critical = "Make these safe to use in SecurityCritical contexts.")]
        [SecurityCritical]
        private readonly T _callback;

        [Tag.SecurityNote(Critical = "Accesses critical field.", Safe = "Data provided by caller.")]
        [SecuritySafeCritical]
        protected Thunk(T callback)
        {
            _callback = callback;
        }

        internal T Callback
        {
            [Tag.SecurityNote(Critical = "Accesses critical field.", Safe = "Data is not privileged.")]
            [SecuritySafeCritical]
            get => _callback;
        }
    }

    internal sealed class ActionThunk<T1> : Thunk<Action<T1>>
    {
        public ActionThunk(Action<T1> callback) : base(callback) { }

        public Action<T1> ThunkFrame => new(UnhandledExceptionFrame);
        [Tag.SecurityNote(Critical = "Calls PrepareConstrainedRegions which has a LinkDemand",
                        Safe = "Guaranteed not to call into PT user code from the finally.")]
        [SecuritySafeCritical]

        private void UnhandledExceptionFrame(T1 result)
        {
            try
            {
                Callback(result);
            }
            catch (Exception exception)
            {
                if (!HandleAtThreadBase(exception))
                {
                    throw;
                }
            }
        }
    }

    internal sealed class AsyncThunk : Thunk<AsyncCallback>
    {
        public AsyncThunk(AsyncCallback callback) : base(callback) { }

        public AsyncCallback ThunkFrame => new(UnhandledExceptionFrame);
        [Tag.SecurityNote(Critical = "Calls PrepareConstrainedRegions which has a LinkDemand",
                        Safe = "Guaranteed not to call into PT user code from the finally.")]
        [SecuritySafeCritical]

        private void UnhandledExceptionFrame(IAsyncResult result)
        {
            try
            {
                Callback(result);
            }
            catch (Exception exception)
            {
                if (!HandleAtThreadBase(exception))
                {
                    throw;
                }
            }
        }
    }

    internal sealed class SendOrPostThunk : Thunk<SendOrPostCallback>
    {
        public SendOrPostThunk(SendOrPostCallback callback) : base(callback) { }

        public SendOrPostCallback ThunkFrame => new(UnhandledExceptionFrame);
        [Tag.SecurityNote(Critical = "Calls PrepareConstrainedRegions which has a LinkDemand",
                        Safe = "Guaranteed not to call into PT user code from the finally.")]
        [SecuritySafeCritical]

        private void UnhandledExceptionFrame(object result)
        {
            try
            {
                Callback(result);
            }
            catch (Exception exception)
            {
                if (!HandleAtThreadBase(exception))
                {
                    throw;
                }
            }
        }
    }

    internal class InternalException : Exception
    {
        public InternalException(string description)
            : base(SR.ShipAssertExceptionMessage(description)) { }
    }

    internal class FatalInternalException : InternalException
    {
        public FatalInternalException(string description)
            : base(description) { }
    }
}
