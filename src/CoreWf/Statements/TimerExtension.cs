// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public abstract class TimerExtension
{
    protected TimerExtension() { }

    public void RegisterTimer(TimeSpan timeout, Bookmark bookmark) => OnRegisterTimer(timeout, bookmark);

    public void CancelTimer(Bookmark bookmark) => OnCancelTimer(bookmark);

    protected abstract void OnRegisterTimer(TimeSpan timeout, Bookmark bookmark);

    protected abstract void OnCancelTimer(Bookmark bookmark);
}
