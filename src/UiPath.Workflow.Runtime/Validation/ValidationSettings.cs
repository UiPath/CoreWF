// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Threading;

namespace System.Activities.Validation;

/// <summary>
/// Represents a collection of settings that customize the behavior that <see cref="ActivityValidationServices.Validate(Activity)"/>
/// will exhibit. It also enables the activity user to apply policy constraints to the workflow.
/// </summary>
[Fx.Tag.XamlVisible(false)]
public class ValidationSettings
{
    private IDictionary<Type, IList<Constraint>> _additionalConstraints;

    /// <summary>
    /// Gets or sets the cancellation token used to notify should the activity be cancelled.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the supplied activity and all the children
    /// and sub-children of the supplied activity are validated, or if the validator should 
    /// validate only to the supplied activity.
    /// </summary>
    public bool SingleLevel { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the root configuration is not subject for 
    /// validation.
    /// </summary>
    public bool SkipValidatingRootConfiguration { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the additional validation constraints are 
    /// to be used exclusively to validate the workflow. If set to true, all the validation 
    /// contained inside the activity itself will be ignored.
    /// </summary>
    public bool OnlyUseAdditionalConstraints { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether this instance is prepared for runtime.
    /// </summary>
    public bool PrepareForRuntime { get; set; }

    /// <summary>
    /// Gets or sets the environment of variables and arguments associated with this 
    /// validation settings.
    /// </summary>
    public LocationReferenceEnvironment Environment { get; set; }

    internal bool HasAdditionalConstraints => _additionalConstraints != null && _additionalConstraints.Count > 0;

    /// <summary>
    /// Gets a dictionary of type-constraint pairs. Each additional constraint added to the 
    /// dictionary will be applied to every activity of the specified type in the workflow to 
    /// validate.
    /// </summary>
    public IDictionary<Type, IList<Constraint>> AdditionalConstraints
    {
        get
        {
            _additionalConstraints ??= new Dictionary<Type, IList<Constraint>>();
            return _additionalConstraints;
        }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the compilation of VB expressions is
    /// skipped during validation. C# expressions are always skipped.
    /// </summary>
    public bool SkipExpressionCompilation { get; set; }

    /// <summary>
    /// Gets or sets a value that tells expression activities to build expressions instead of 
    /// only validating the expression for errors.
    /// </summary>
    /// <remarks>
    /// Defaulting to true until validation path is proven.
    /// </remarks>
    public bool ForceExpressionCache { get; set; } = true;

    /// <summary>
    /// Gets or sets a value that indicates whether skipping validation/metadata caching for implementation children.
    /// Implementation children are usually encapsulated, and are considered an implementation detail of an activity, so
    /// in particular situation (such as design-time validation), validating them is not necessary.
    /// </summary>
    public bool SkipImplementationChildren { get; set; } = false;
}
