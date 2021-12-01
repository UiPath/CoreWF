// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Threading;

namespace System.Activities.Validation;

[Fx.Tag.XamlVisible(false)]
public class ValidationSettings
{
    private IDictionary<Type, IList<Constraint>> _additionalConstraints;

    public CancellationToken CancellationToken { get; set; }

    public bool SingleLevel { get; set; }

    public bool SkipValidatingRootConfiguration { get; set; }

    public bool OnlyUseAdditionalConstraints { get; set; }

    public bool PrepareForRuntime { get; set; }

    public LocationReferenceEnvironment Environment { get; set; }

    internal bool HasAdditionalConstraints => _additionalConstraints != null && _additionalConstraints.Count > 0;

    public IDictionary<Type, IList<Constraint>> AdditionalConstraints
    {
        get
        {
            _additionalConstraints ??= new Dictionary<Type, IList<Constraint>>(); 
            return _additionalConstraints;
        }
    }

    public bool SkipExpressionCompilation { get; set; }
}
