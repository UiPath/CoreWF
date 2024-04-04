// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace TestObjects.Xaml.GraphCore
{
    public interface ITestDependencyObject
    {
        IDictionary<string, object> Properties { get; }
        object GetValue(XName p);
        void SetValue(XName p, object value);
    }
}
