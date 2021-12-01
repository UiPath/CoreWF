// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public class Reference<TLocation> : CodeActivity<Location<TLocation>>
{
    private readonly string _locationName;
    public Reference(string locationName) => _locationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
    protected override Location<TLocation> Execute(CodeActivityContext context)
    {
        try
        {
            context.AllowChainedEnvironmentAccess = true;
            return context.GetLocation<TLocation>(_locationName);
        }
        finally
        {
            context.AllowChainedEnvironmentAccess = false;
        }
    }
}
