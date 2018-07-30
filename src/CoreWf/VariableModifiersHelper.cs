// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Internals;
    using System.ComponentModel;

    internal static class VariableModifiersHelper
    {
        private static bool IsDefined(VariableModifiers modifiers)
        {
            return (modifiers == VariableModifiers.None ||
                ((modifiers & (VariableModifiers.Mapped | VariableModifiers.ReadOnly)) == modifiers));
        }

        public static bool IsReadOnly(VariableModifiers modifiers)
        {
            return (modifiers & VariableModifiers.ReadOnly) == VariableModifiers.ReadOnly;
        }

        public static bool IsMappable(VariableModifiers modifiers)
        {
            return (modifiers & VariableModifiers.Mapped) == VariableModifiers.Mapped;
        }

        public static void Validate(VariableModifiers modifiers, string argumentName)
        {
            if (!IsDefined(modifiers))
            {
                throw FxTrace.Exception.AsError(
                    new InvalidEnumArgumentException(argumentName, (int)modifiers, typeof(VariableModifiers)));
            }
        }
    }
}
