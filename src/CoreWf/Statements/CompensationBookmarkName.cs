// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace CoreWf.Statements
{
    internal enum CompensationBookmarkName
    {
        Confirmed = 0,
        Canceled = 1,
        Compensated = 2,
        OnConfirmation = 3,
        OnCompensation = 4,
        OnCancellation = 5,
        OnSecondaryRootScheduled = 6,
    }
}
