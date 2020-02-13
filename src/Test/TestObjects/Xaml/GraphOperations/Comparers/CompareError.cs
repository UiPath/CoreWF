// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using TestObjects.Xaml.GraphCore;

namespace TestObjects.Xaml.GraphOperations.Comparers
{
    [Serializable]
    public class CompareError
    {
        public ObjectGraph Node1;
        public ObjectGraph Node2;
        public Exception Error;

        public CompareError(ObjectGraph node1, ObjectGraph node2, Exception exception)
        {
            this.Node1 = node1;
            this.Node2 = node2;
            this.Error = exception;
        }

        public override string ToString()
        {
            string mesg = string.Format("Compare Error: {0}; Node1:{1}; Node2:{2}", this.Error.Message, this.Node1, this.Node2);
            return mesg;
        }

        public static CompareError GetCompareError(TestDependencyObject dependencyObject)
        {
            return (CompareError)dependencyObject.GetValue(ObjectGraphComparer.CompareErrorProperty);
        }
    }
}
