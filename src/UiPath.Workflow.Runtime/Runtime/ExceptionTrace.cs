// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Activities.Runtime;

internal class ExceptionTrace
{
    private readonly string _eventSourceName;

    public ExceptionTrace(string eventSourceName)
    {
        _eventSourceName = eventSourceName;
    }

    public static void AsInformation(Exception exception)
    {
        if (WfEventSource.Instance.HandledExceptionIsEnabled())
            WfEventSource.Instance.HandledException(exception.Message, exception.ToString());
    }

    public static void AsWarning(Exception exception)
    {
        if (WfEventSource.Instance.HandledExceptionWarningIsEnabled())
            WfEventSource.Instance.HandledExceptionWarning(exception.Message, exception.ToString());
    }

    public Exception AsError(Exception exception)
    {
        // AggregateExceptions are automatically unwrapped.
        if (exception is AggregateException aggregateException)
        {
            return AsError<Exception>(aggregateException);
        }

        // TargetInvocationExceptions are automatically unwrapped.
        if (exception is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null)
        {
            return AsError(targetInvocationException.InnerException);
        }

        return TraceException(exception);
    }

    public Exception AsError(Exception exception, string eventSource)
    {
        // AggregateExceptions are automatically unwrapped.
        if (exception is AggregateException aggregateException)
        {
            return ExceptionTrace.AsError<Exception>(aggregateException, eventSource);
        }

        // TargetInvocationExceptions are automatically unwrapped.
        if (exception is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null)
        {
            return AsError(targetInvocationException.InnerException, eventSource);
        }

        return TraceException(exception, eventSource);
    }

    public Exception AsError(TargetInvocationException targetInvocationException, string eventSource)
    {
        Fx.Assert(targetInvocationException != null, "targetInvocationException cannot be null.");

        // If targetInvocationException contains any fatal exceptions, return it directly
        // without tracing it or any inner exceptions.
        if (Fx.IsFatal(targetInvocationException))
        {
            return targetInvocationException;
        }

        // A non-null inner exception could require further unwrapping in AsError.
        Exception innerException = targetInvocationException.InnerException;
        if (innerException != null)
        {
            return AsError(innerException, eventSource);
        }

        // A null inner exception is unlikely but possible.
        // In this case, trace and return the targetInvocationException itself.
        return TraceException<Exception>(targetInvocationException, eventSource);
    }

    public Exception AsError<TPreferredException>(AggregateException aggregateException)
    {
        return AsError<TPreferredException>(aggregateException, _eventSourceName);
    }

    /// <summary>
    /// Extracts the first inner exception of type <typeparamref name="TPreferredException"/>
    /// from the <see cref="AggregateException"/> if one is present.
    /// </summary>
    /// <remarks>
    /// If no <typeparamref name="TPreferredException"/> inner exception is present, this
    /// method returns the first inner exception.   All inner exceptions will be traced,
    /// including the one returned.   The containing <paramref name="aggregateException"/>
    /// will not be traced unless there are no inner exceptions.
    /// </remarks>
    /// <typeparam name="TPreferredException">The preferred type of inner exception to extract.   
    /// Use <c>typeof(Exception)</c> to extract the first exception regardless of type.</typeparam>
    /// <param name="aggregateException">The <see cref="AggregateException"/> to examine.</param>
    /// <param name="eventSource">The event source to trace.</param>
    /// <returns>The extracted exception.  It will not be <c>null</c> 
    /// but it may not be of type <typeparamref name="TPreferredException"/>.</returns>
    public static Exception AsError<TPreferredException>(AggregateException aggregateException, string eventSource)
    {
        Fx.Assert(aggregateException != null, "aggregateException cannot be null.");

        // If aggregateException contains any fatal exceptions, return it directly
        // without tracing it or any inner exceptions.
        if (Fx.IsFatal(aggregateException))
        {
            return aggregateException;
        }

        // Collapse possibly nested graph into a flat list.
        // Empty inner exception list is unlikely but possible via public api.
        ReadOnlyCollection<Exception> innerExceptions = aggregateException.Flatten().InnerExceptions;
        if (innerExceptions.Count == 0)
        {
            return TraceException(aggregateException, eventSource);
        }

        // Find the first inner exception, giving precedence to TPreferredException
        Exception favoredException = null;
        foreach (Exception nextInnerException in innerExceptions)
        {
            // AggregateException may wrap TargetInvocationException, so unwrap those as well

            Exception innerException = (nextInnerException is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null)
                                            ? targetInvocationException.InnerException
                                            : nextInnerException;

            if (innerException is TPreferredException && favoredException == null)
            {
                favoredException = innerException;
            }

            // All inner exceptions are traced
            TraceException(innerException, eventSource);
        }

        if (favoredException == null)
        {
            Fx.Assert(innerExceptions.Count > 0, "InnerException.Count is known to be > 0 here.");
            favoredException = innerExceptions[0];
        }

        return favoredException;
    }

    public ArgumentException Argument(string paramName, string message)
    {
        return TraceException(new ArgumentException(message, paramName));
    }

    public ArgumentNullException ArgumentNull(string paramName)
    {
        return TraceException(new ArgumentNullException(paramName));
    }

    public ArgumentNullException ArgumentNull(string paramName, string message)
    {
        return TraceException(new ArgumentNullException(paramName, message));
    }

    public ArgumentException ArgumentNullOrEmpty(string paramName)
    {
        return Argument(paramName, SR.ArgumentNullOrEmpty(paramName));
    }

    public ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object actualValue, string message)
    {
        return TraceException(new ArgumentOutOfRangeException(paramName, actualValue, message));
    }

    public static void TraceUnhandledException(Exception exception)
    {
        if (WfEventSource.Instance.UnhandledExceptionIsEnabled())
            WfEventSource.Instance.UnhandledException(exception.Message, exception.ToString());
    }

    private TException TraceException<TException>(TException exception)
        where TException : Exception
    {
        return TraceException(exception, _eventSourceName);
    }

    [ResourceConsumption(ResourceScope.Process)]
    private static TException TraceException<TException>(TException exception, string eventSource)
                where TException : Exception
    {
        if (WfEventSource.Instance.ThrowingExceptionIsEnabled())
        {
            WfEventSource.Instance.ThrowingException(eventSource, exception.Message, exception.ToString());
        }

        BreakOnException(exception);

        return exception;
    }

    private static void BreakOnException(Exception exception)
    {
#if DEBUG
        if (Fx.BreakOnExceptionTypes != null)
        {
            foreach (Type breakType in Fx.BreakOnExceptionTypes)
            {
                if (breakType.IsAssignableFrom(exception.GetType()))
                {
                    // This is intended to "crash" the process so that a debugger can be attached.  If a managed
                    // debugger is already attached, it will already be able to hook these exceptions.  We don't
                    // want to simulate an unmanaged crash (DebugBreak) in that case.
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {

                        System.Diagnostics.Debugger.Break();
                    }
                }
            }
        }
#endif
    }
}
