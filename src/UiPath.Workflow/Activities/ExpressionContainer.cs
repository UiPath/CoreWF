using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Activities;

/// <summary>
///     Contains all the relevant information regarding the validation of an expression.
/// </summary>
public class ExpressionContainer
{
    /// <summary>
    ///     The type returned from the expression.
    /// </summary>
    public Type ResultType { get; set; }

    /// <summary>
    ///     The current compilation object.
    /// </summary>
    public Compilation CompilationUnit { get; set; }

    /// <summary>
    ///     Expression text, imported namespaces, and variable type getter function.
    /// </summary>
    public ExpressionToCompile ExpressionToValidate { get; set; }

    /// <summary>
    ///     Activity that owns the expression.
    /// </summary>
    public Activity CurrentActivity { get; set; }

    /// <summary>
    ///     LRE to contain the in-scope identifiers.
    /// </summary>
    public LocationReferenceEnvironment Environment { get; set; }

    /// <summary>
    ///     Assemblies required to validate the expression.
    /// </summary>
    public ICollection<Assembly> RequiredAssemblies { get; set; }

    /// <summary>
    ///     Diagnostics reported by validating the expression.
    /// </summary>
    public IEnumerable<TextExpressionCompilerError> Diagnostics { get; set; }

    /// <summary>
    /// populated before CreateValidationCode step
    /// </summary>
    public (string Name, Type Type)[] ResolvedIdentifiers { get; set; }
}
