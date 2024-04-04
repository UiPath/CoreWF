// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.Runtime
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
