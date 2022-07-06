// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;

internal static class ArgumentDirectionHelper
{
    internal static bool IsDefined(ArgumentDirection direction) => direction is ArgumentDirection.In or ArgumentDirection.Out or ArgumentDirection.InOut;

    public static void Validate(ArgumentDirection direction, string argumentName)
    {
        if (!IsDefined(direction))
        {
            throw FxTrace.Exception.AsError(
                new InvalidEnumArgumentException(argumentName, (int)direction, typeof(ArgumentDirection)));
        }
    }

    public static bool IsIn(Argument argument) => IsIn(argument.Direction);

    public static bool IsIn(ArgumentDirection direction) => direction is ArgumentDirection.In or ArgumentDirection.InOut;

    public static bool IsOut(Argument argument) => IsOut(argument.Direction);

    public static bool IsOut(ArgumentDirection direction) => direction is ArgumentDirection.Out or ArgumentDirection.InOut;

    public static Exception NoOutputLocation(RuntimeArgument argument) => FxTrace.Exception.AsError(new InvalidOperationException(SR.NoOutputLocationWasFound(argument.Name)));
}
