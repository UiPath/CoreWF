// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Microsoft.CoreWf
{
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(
                    new InvalidEnumArgumentException(argumentName, (int)modifiers, typeof(VariableModifiers)));
            }
        }
    }
}
