// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Expressions;

/// <summary>
/// Represents settings that control how expressions are evaluated at runtime.
/// </summary>
[Fx.Tag.XamlVisible(false)]
public class ExpressionEvaluationSettings
{
    /// <summary>
    /// Gets or sets a value that indicates whether lambda expressions should prefer interpretation over compilation.
    /// When true, lambda expressions will be interpreted at runtime rather than compiled to IL. 
    /// This can be faster, especially for one-time only evaluation.
    /// </summary>
    public bool PreferExpressionInterpretation { get; set; }
} 