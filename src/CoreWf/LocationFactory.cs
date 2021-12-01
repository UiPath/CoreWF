// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

internal abstract class LocationFactory
{
    public Location CreateLocation(ActivityContext context) => CreateLocationCore(context);

    protected abstract Location CreateLocationCore(ActivityContext context);
}

internal abstract class LocationFactory<T> : LocationFactory
{
    public abstract new Location<T> CreateLocation(ActivityContext context);

    protected override Location CreateLocationCore(ActivityContext context) => CreateLocation(context);
}
