// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Test.Common.TestObjects.Runtime
{
    // [Serializable]
    public class TestSymbolResolver
    {
        private IDictionary<string, object> _data;

        public TestSymbolResolver()
        {
            _data = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }
}
