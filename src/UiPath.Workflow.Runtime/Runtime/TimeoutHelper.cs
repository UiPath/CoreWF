// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities.Runtime;

internal struct TimeoutHelper
{
    private DateTime _deadline;
    private bool _deadlineSet;
    private readonly TimeSpan _originalTimeout;
    public static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(int.MaxValue);

    public TimeoutHelper(TimeSpan timeout)
    {
        Fx.Assert(timeout >= TimeSpan.Zero, "timeout must be non-negative");

        _originalTimeout = timeout;
        _deadline = DateTime.MaxValue;
        _deadlineSet = (timeout == TimeSpan.MaxValue);
    }

    public TimeSpan OriginalTimeout => _originalTimeout;

    public static DateTime Add(DateTime time, TimeSpan timeout)
    {
        if (timeout >= TimeSpan.Zero && DateTime.MaxValue - time <= timeout)
        {
            return DateTime.MaxValue;
        }
        if (timeout <= TimeSpan.Zero && DateTime.MinValue - time >= timeout)
        {
            return DateTime.MinValue;
        }
        return time + timeout;
    }

    public TimeSpan RemainingTime()
    {
        if (!_deadlineSet)
        {
            SetDeadline();
            return _originalTimeout;
        }
        else if (_deadline == DateTime.MaxValue)
        {
            return TimeSpan.MaxValue;
        }
        else
        {
            TimeSpan remaining = _deadline - DateTime.UtcNow;
            return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }

    private void SetDeadline()
    {
        Fx.Assert(!_deadlineSet, "TimeoutHelper deadline set twice.");
        _deadline = DateTime.UtcNow + _originalTimeout;
        _deadlineSet = true;
    }

    public static void ThrowIfNegativeArgument(TimeSpan timeout) => ThrowIfNegativeArgument(timeout, "timeout");

    public static void ThrowIfNegativeArgument(TimeSpan timeout, string argumentName)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw Fx.Exception.ArgumentOutOfRange(argumentName, timeout, SR.TimeoutMustBeNonNegative(argumentName, timeout));
        }
    }

    [Fx.Tag.Blocking]
    public static bool WaitOne(WaitHandle waitHandle, TimeSpan timeout)
    {
        ThrowIfNegativeArgument(timeout);
        if (timeout == TimeSpan.MaxValue)
        {
            waitHandle.WaitOne();
            return true;
        }
        else
        {
            return waitHandle.WaitOne(timeout);
        }
    }
}
