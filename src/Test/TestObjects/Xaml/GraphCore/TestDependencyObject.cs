using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TestObjects.Xaml.GraphCore
{
    [Serializable]
    public class TestDependencyObject : ITestDependencyObject
    {

        Dictionary<string, object> storage = new Dictionary<string, object>();


        public IDictionary<string, object> Properties
        {
            get
            {
                return this.storage;
            }
        }

        public virtual void SetValue(XName p, object value)
        {
            if (this.storage.Keys.Contains(p.LocalName))
            {
                this.storage[p.LocalName] = value;
            }
            else
            {
                this.storage.Add(p.LocalName, value);
            }
        }

        public virtual object GetValue(XName p)
        {
            if (this.storage.Keys.Contains(p.LocalName))
            {
                return this.storage[p.LocalName];
            }
            else
            {
                return null;
            }
        }
    }

}
