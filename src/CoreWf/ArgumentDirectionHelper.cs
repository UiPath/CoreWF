// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.ComponentModel;

    internal static class ArgumentDirectionHelper
    {
        internal static bool IsDefined(ArgumentDirection direction)
        {
            return (direction == ArgumentDirection.In || direction == ArgumentDirection.Out || direction == ArgumentDirection.InOut);
        }

        public static void Validate(ArgumentDirection direction, string argumentName)
        {
            if (!IsDefined(direction))
            {
                throw FxTrace.Exception.AsError(
                    new InvalidEnumArgumentException(argumentName, (int)direction, typeof(ArgumentDirection)));
            }
        }

        public static bool IsIn(Argument argument)
        {
            return ArgumentDirectionHelper.IsIn(argument.Direction);
        }

        public static bool IsIn(ArgumentDirection direction)
        {
            return (direction == ArgumentDirection.In) || (direction == ArgumentDirection.InOut);
        }

        public static bool IsOut(Argument argument)
        {
            return ArgumentDirectionHelper.IsOut(argument.Direction);
        }

        public static bool IsOut(ArgumentDirection direction)
        {
            return (direction == ArgumentDirection.Out) || (direction == ArgumentDirection.InOut);
        }
    }
}
