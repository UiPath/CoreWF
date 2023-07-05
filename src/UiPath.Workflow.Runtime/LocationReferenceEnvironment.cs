// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public abstract class LocationReferenceEnvironment
{
    protected LocationReferenceEnvironment() { }

    internal bool CompileExpressions { get; set; }

    /// <summary>
    /// Indicates if this LRE is created as part of activity validation.
    /// </summary>
    internal bool IsValidating { get; set; }

    public abstract Activity Root { get; }

    public LocationReferenceEnvironment Parent { get; protected set; }

    public abstract bool IsVisible(LocationReference locationReference);

    public abstract bool TryGetLocationReference(string name, out LocationReference result);

    public abstract IEnumerable<LocationReference> GetLocationReferences();
}
