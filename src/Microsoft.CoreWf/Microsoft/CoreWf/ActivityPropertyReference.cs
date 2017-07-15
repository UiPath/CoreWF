// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf
{
    public class ActivityPropertyReference
    {
        public ActivityPropertyReference()
        {
        }

        public string SourceProperty
        {
            get;
            set;
        }

        public string TargetProperty
        {
            get;
            set;
        }
    }
}
