using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
//using TestObjects.Utilities; // Need to address Log.TraceInternal() calls for logging errors with comparison in another way
using TestObjects.Xaml.GraphCore;
using TestObjects.Xaml.GraphOperations.Builders;
using TestObjects.XamlObjectComparer;

namespace TestObjects.Xaml.GraphOperations.Comparers
{
    public class ObjectGraphComparer
    {
        public static XName CompareModeProperty = XName.Get("CompareMode", "");
        public static XName CompareErrorProperty = XName.Get("CompareError", "");

        /// <summary>
        /// Helper for testing parity with the TreeComparer
        /// </summary>
        /// <param name="root1"></param>
        /// <param name="root2"></param>
        /// <param name="v1CompareResults"></param>
        public static void XamlCompareParity(Object root1, Object root2, CompareResult v1CompareResults)
        {
            GraphCompareResults compareResults = ObjectGraphComparer.XamlCompare(root1, root2);

            bool traceGraphs = false;
            // If results dont match between the two comparers //
            if ((compareResults.Passed == true && v1CompareResults == CompareResult.Different) ||
                (compareResults.Passed == false && v1CompareResults == CompareResult.Equivalent))
            {
                //Log.TraceInternal("Results do not match between XamlTreeComparer and ObjectGraphComparer");
                traceGraphs = true;
            }

            // If it was a failure or the compares dont match //
            if (v1CompareResults == CompareResult.Different || traceGraphs == true)
            {
                string tmpName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                //Log.TraceInternal("Logging object graphs - " + tmpName + "*.xml");

                ObjectGraph.Serialize(ObjectGraphWalker.Create(root1), tmpName + "root1.xml");
                ObjectGraph.Serialize(ObjectGraphWalker.Create(root2), tmpName + "root2.xml");
                ObjectGraph.Serialize(compareResults.ResultGraph, tmpName + "result.xml");

                //Log.TraceInternal("Exceptions are as follows:");

                foreach (CompareError error in compareResults.Errors)
                {
                    //Log.TraceInternal(string.Format("Node: {0}  Message= {1}", error.Node1.QualifiedName, error.Error.Message));
                }
            }
        }


        public static GraphCompareResults XamlCompare(Object root1, Object root2)
        {
            ObjectGraph graph1 = ObjectGraphWalker.Create(root1);
            ObjectGraph graph2 = ObjectGraphWalker.Create(root2);

            return ObjectGraphComparer.Compare(graph1, graph2, null);
        }


        // TODO: Ignore story //
        public static GraphCompareResults Compare(ObjectGraph root1, ObjectGraph root2)
        {
            // search and set the ignores on root1 //

            return ObjectGraphComparer.Compare(root1, root2, null);
        }

        public static GraphCompareResults Compare(ObjectGraph root1, ObjectGraph root2, List<Object> Ignores)
        {
            // Do the comparison here //
            List<IGraphNode> nodes1 = root1.Decendants;
            List<IGraphNode> nodes2 = root2.Decendants;

            GraphCompareResults compareResults = new GraphCompareResults();

            ObjectGraph compareResultGraph = root1.Clone();
            List<IGraphNode> resultTreeNodes = compareResultGraph.Decendants;

            if (nodes1.Count != nodes2.Count)
            {
                CompareError error = new CompareError((ObjectGraph)nodes1[0], (ObjectGraph)nodes2[0], new Exception("Number of nodes do not match"));
                resultTreeNodes[0].SetValue(ObjectGraphComparer.CompareErrorProperty, error);
                compareResults.Errors.Add(error);
            }

            for (int i = 0; i < nodes1.Count; i++)
            {
                ObjectGraph node1 = (ObjectGraph)nodes1[i];

                var nodelist = from node in nodes2
                               where node1.QualifiedName.Equals(node.QualifiedName)
                               select node;

                List<IGraphNode> matchingNodes = nodelist.ToList<IGraphNode>();
                if (matchingNodes.Count == 0)
                {
                    CompareError error = new CompareError(node1, null, new Exception("Node not present in second tree"));
                    compareResults.Errors.Add(error);
                    resultTreeNodes[i].SetValue(ObjectGraphComparer.CompareErrorProperty, error);
                    continue;
                }
                if (matchingNodes.Count > 1)
                {
                    CompareError error = new CompareError(node1, null, new Exception("more than one match for this node in second tree"));
                    compareResults.Errors.Add(error);
                    resultTreeNodes[i].SetValue(ObjectGraphComparer.CompareErrorProperty, error);
                    continue;
                }

                ObjectGraph node2 = (ObjectGraph)matchingNodes[0];

                CompareError error1 = ObjectGraphComparer.CompareNodes(node1, node2);
                if (error1 != null)
                {
                    compareResults.Errors.Add(error1);
                    resultTreeNodes[i].SetValue(ObjectGraphComparer.CompareErrorProperty, error1);
                }

            }

            compareResults.Passed = compareResults.Errors.Count == 0 ? true : false;
            compareResults.ResultGraph = compareResultGraph;
            return compareResults;
        }

        private static CompareError CompareNodes(ObjectGraph node1, ObjectGraph node2)
        {
            // Compare two nodes - just the nodes //
            // - compare the property name
            // - compare the property value
            object oMode = node1.GetValue(ObjectGraphComparer.CompareModeProperty);
            if (oMode == null)
            {
                oMode = CompareMode.PropertyNameAndValue;
            }

            CompareMode mode = (CompareMode)oMode;

            // default is compare name and value //

            if (mode == CompareMode.PropertyNameAndValue || mode == CompareMode.PropertyName)
            {
                // comapre name //
                if (!node1.Name.Equals(node2.Name))
                {
                    CompareError error = new CompareError(node1, node2, new Exception("Node names do not match"));
                    return error;
                }
            }

            if (mode == CompareMode.PropertyNameAndValue || mode == CompareMode.PropertyValue)
            {
                // compare values - only compare if they are primitive types, if it is a complex
                // type, its properties will be child nodes in the metadata graph 


                if (node1.DataType.IsPrimitive)
                {

                    ValueType value1 = node1.Data as ValueType;
                    ValueType value2 = node2.Data as ValueType;
                    // Need special handling for double and float as well as strings //
                    if (!ObjectGraphComparer.ComparePrimitive(value1, value2))
                    {
                        CompareError error = new CompareError(node1, node2, new Exception("Node values do not match"));
                        return error;
                    }
                    return null;
                }

                // string is not a primitive type // 
                if (node1.DataType == typeof(String))
                { // for string case //

                    if (node1.Data == null && node2.Data == null)
                    {
                        return null;
                    }

                    if (node1.Data == null || node2.Data == null)
                    {
                        CompareError error = new CompareError(node1, node2, new Exception("Node values do not match"));
                        return error;
                    }

                    if (!node1.Data.Equals(node2.Data))
                    {
                        CompareError error = new CompareError(node1, node2, new Exception("Node values do not match"));
                        return error;
                    }
                }
            }

            return null;

        }

        // <summary>
        // Compare two value types
        // </summary>
        // <param name="obj1">The first value</param>
        // <param name="obj2">The second value</param>
        // <returns>
        // true, if they are the same
        // false, otherwise
        // </returns>
        private static bool ComparePrimitive(object obj1, object obj2)
        {
            bool same = false;
            double ErrorAllowed = 0.000001;

            if (obj1 == null || obj2 == null)
            {
                // Due to properties that return exceptions in TreeComparer.CompareClrProperty,
                // The 'value' of these properties will be null and therefore must be handled.
                same = (obj1 == obj2);
            }
            // for double or float comparison, certain error should be allowed. 
            else if (obj1 is double)
            {
                double double1 = (double)obj1;
                double double2 = (double)obj2;
                if ((obj1.Equals(double.NaN) && obj2.Equals(double.NaN))
                    || (double.IsInfinity(double1) && double.IsInfinity(double2))
                )
                {
                    return true;
                }

                same = Math.Abs(double2) > ErrorAllowed ?
                    (double1 / double2) > (1 - ErrorAllowed) && (double1 / double2) < (1 + ErrorAllowed) :
                    Math.Abs(double1 - double2) < ErrorAllowed;
            }
            else if (obj1 is float)
            {
                float float1 = (float)obj1;
                float float2 = (float)obj2;
                if ((obj1.Equals(float.NaN) && obj2.Equals(float.NaN))
                    || (float.IsInfinity(float1) && float.IsInfinity(float2))
                )
                {
                    return true;
                }

                same = Math.Abs(float2) > ErrorAllowed ?
                    (float1 / float2) > (1 - ErrorAllowed) && (float1 / float2) < (1 + ErrorAllowed) :
                    Math.Abs(float1 - float2) < ErrorAllowed;
            }
            else
            {
                same = obj1.Equals(obj2);
            }

            return same;
        }
    }
}
