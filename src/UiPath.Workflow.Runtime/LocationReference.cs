// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public abstract class LocationReference
{
    protected LocationReference() { }

    public string Name => this.NameCore;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.PropertyNamesShouldNotMatchGetMethods,
    //    Justification = "Workflow normalizes on Type for Type properties")]
    public Type Type => this.TypeCore;

    // internal Id use for arguments/variables/delegate arguments, and accessed
    // by our mapping pieces
    internal int Id { get; set; }

    protected abstract string NameCore { get; }
    protected abstract Type TypeCore { get; }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public abstract Location GetLocation(ActivityContext context);

    // The contract of this method is the caller promises never to write to the location
    internal virtual Location GetLocationForRead(ActivityContext context) => GetLocation(context);

    // The contract of this method is the caller promises never to read from the location
    internal virtual Location GetLocationForWrite(ActivityContext context) => GetLocation(context);
}
