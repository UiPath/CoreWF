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
