// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions;

public sealed class VariableReference<T> : EnvironmentLocationReference<T>
{
    public VariableReference()
        : base() { }

    public VariableReference(Variable variable)
        : base()
    {
        Variable = variable;
    }

    public Variable Variable { get; set; }

    public override LocationReference LocationReference => Variable;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        if (Variable == null)
        {
            metadata.AddValidationError(SR.VariableMustBeSet);
        }
        else
        {
            if (Variable is not Variable<T>)
            {
                metadata.AddValidationError(SR.VariableTypeInvalid(Variable, typeof(T), Variable.Type));
            }

            if (!Variable.IsInTree)
            {
                metadata.AddValidationError(SR.VariableShouldBeOpen(Variable.Name));
            }

            if (!metadata.Environment.IsVisible(Variable))
            {
                metadata.AddValidationError(SR.VariableNotVisible(Variable.Name));
            }

            if (VariableModifiersHelper.IsReadOnly(Variable.Modifiers))
            {
                metadata.AddValidationError(SR.VariableIsReadOnly(Variable.Name));
            }
        }
    }

    public override string ToString()
    {
        if (Variable != null && !string.IsNullOrEmpty(Variable.Name))
        {
            return Variable.Name;
        }

        return base.ToString();
    }
}
