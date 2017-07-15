// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf
{
    internal abstract class LocationFactory
    {
        public Location CreateLocation(ActivityContext context)
        {
            return CreateLocationCore(context);
        }

        protected abstract Location CreateLocationCore(ActivityContext context);
    }

    internal abstract class LocationFactory<T> : LocationFactory
    {
        public abstract new Location<T> CreateLocation(ActivityContext context);

        protected override Location CreateLocationCore(ActivityContext context)
        {
            return this.CreateLocation(context);
        }
    }
}
