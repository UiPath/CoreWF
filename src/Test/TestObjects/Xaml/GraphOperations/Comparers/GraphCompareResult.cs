// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace TestObjects.Xaml.GraphOperations.Comparers
{
    [Serializable]
    public class GraphCompareResults
    {
        public List<CompareError> Errors = new List<CompareError>();
        public ObjectGraph ResultGraph = null;
        public bool Passed = false;
    }
}
