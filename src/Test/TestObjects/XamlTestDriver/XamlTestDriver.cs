// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Statements;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xaml;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Utilities;
using TestObjects.XamlObjectComparer;

namespace TestObjects.XamlTestDriver
{
    public class XamlTestDriver
    {

        public static object RoundTripAndCompareObjects(object obj, params string[] propertyNamesToBeIgnored)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            MemoryStream xamlStream = null;
            object roundTrippedObject = XamlTestDriver.RoundTrip(obj, out xamlStream);

            using (xamlStream)
            {

                Dictionary<string, PropertyToIgnore> ignore = new Dictionary<string, PropertyToIgnore>();
                foreach (string propertyName in propertyNamesToBeIgnored)
                {
                    ignore.Add(propertyName, new PropertyToIgnore() { WhatToIgnore = IgnoreProperty.IgnoreValueOnly });
                }

                TreeComparerResult result;
                if (ignore.Count == 0)
                {
                    result = TreeComparer.CompareLogical(obj, roundTrippedObject);
                }
                else
                {
                    result = TreeComparer.CompareLogical(obj, roundTrippedObject, ignore);
                }

                if (result.Result == CompareResult.Different)
                {
                    string source = new ObjectDumper().DumpToString(null, obj);
                    string target = new ObjectDumper().DumpToString(null, roundTrippedObject);
                    XamlTestDriver.TraceXamlFile(xamlStream);
                    throw new Exception("Two objects are different.");
                }

                return roundTrippedObject;
            }
        }

        public static object RoundTripAndExamineXAML(object obj, string[] xPathExpressions, XmlNamespaceManager namespaceManager)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (xPathExpressions == null)
            {
                throw new ArgumentNullException("xPathExpressions");
            }

            MemoryStream xamlStream;
            object roundTrippedObject = XamlTestDriver.RoundTrip(obj, out xamlStream);

            using (xamlStream)
            {
                XPathDocument doc = new XPathDocument(xamlStream);
                XPathNavigator navigator = doc.CreateNavigator();
                foreach (string xPathExpression in xPathExpressions)
                {
                    XPathNodeIterator iterator = null;

                    if (namespaceManager != null)
                    {
                        iterator = navigator.Select(xPathExpression, namespaceManager);
                    }
                    else
                    {
                        iterator = navigator.Select(xPathExpression);
                    }

                    if (iterator == null)
                    {
                        //Log.TraceInternal("XAML file generated during serializing: ");
                        XamlTestDriver.TraceXamlFile(xamlStream);
                        throw new Exception(string.Format("The xpath '{0}' does not map to any node in the above xaml document.", xPathExpression));
                    }

                }

                return roundTrippedObject;
            }
        }

        public static void ModifyAndValidateException(TestActivity testActivity, Type exceptionType, string errorString)
        {
            string originalXaml = XamlTestDriver.Serialize(testActivity.ProductActivity);
            string modifiedXaml = testActivity.ModifyXamlDelegate(originalXaml);
            ExceptionHelpers.CheckForException(exceptionType, errorString, () => Deserialize(modifiedXaml));
        }

        public static object RoundTripAndModifyXaml(object obj, ModifyXaml modifyXaml)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (modifyXaml == null)
            {
                throw new ArgumentNullException("modifyXaml");
            }

            using (MemoryStream xamlStream = new MemoryStream())
            {
                string modifiedXaml = string.Empty;
                try
                {

                    Serialize(obj, xamlStream);
                    modifiedXaml = modifyXaml(GetStringFromMemoryStream(xamlStream));

                    using (XmlReader reader = XmlReader.Create(new StringReader(modifiedXaml)))
                    {
                        return Deserialize(reader);
                    }
                }
                catch (Exception) // jasonv - approved; adds useful information and rethrows
                {
                    //Log.TraceInternal("Exception while roundtripping: ");
                    //Log.TraceInternal("XAML file before modification: ");
                    TraceXamlFile(xamlStream);
                    //Log.TraceInternal("XAML file after modification: ");
                    TraceXamlFile(modifiedXaml);

                    throw;
                }
            }
        }

        public static string AddAttribute(string xaml, string xpath, XAttribute attr)
        {
            return XamlTestDriver.AddAttribute(xaml, xpath, attr, null);
        }

        public static string AddAttribute(string xaml, string xpath, XAttribute attr, IEnumerable<KeyValuePair<string, string>> namespaceMappings)
        {
            return XamlTestDriver.AddObject(xaml, xpath, attr, namespaceMappings);
        }

        public static string RemoveAttribute(string xaml, string xpath)
        {
            return XamlTestDriver.RemoveAttribute(xaml, xpath, null);
        }

        public static string RemoveAttribute(string xaml, string xpath, IEnumerable<KeyValuePair<string, string>> namespaceMappings)
        {
            return XamlTestDriver.ExecuteQuery<XAttribute>(xaml, xpath, null, namespaceMappings, (result, notUsed) => result.Remove());
        }

        public static string AddNode(string xaml, string xpath, XElement node)
        {
            return XamlTestDriver.AddNode(xaml, xpath, node, null);
        }

        public static string AddNode(string xaml, string xpath, XElement node, IEnumerable<KeyValuePair<string, string>> namespaceMappings)
        {
            return XamlTestDriver.AddObject(xaml, xpath, node, namespaceMappings);
        }

        public static string RemoveNode(string xaml, string xpath)
        {
            return XamlTestDriver.RemoveNode(xaml, xpath, null);
        }

        public static string RemoveNode(string xaml, string xpath, IEnumerable<KeyValuePair<string, string>> namespaceMappings)
        {
            return XamlTestDriver.ExecuteQuery<XNode>(xaml, xpath, null, namespaceMappings, (result, notUsed) => result.Remove());
        }

        private static string AddObject<DataType>(string xaml, string xpath, DataType data, IEnumerable<KeyValuePair<string, string>> namespaceMappings)
        {
            return XamlTestDriver.ExecuteQuery<XContainer>(xaml, xpath, data, namespaceMappings, (container, dataToAdd) => container.Add(dataToAdd));
        }

        private static string ExecuteQuery<QueryResultType>(string xaml, string xpath, object data, IEnumerable<KeyValuePair<string, string>> namespaceMappings, Action<QueryResultType, object> action)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(xaml)))
            {
                XDocument doc = XDocument.Load(reader);
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(reader.NameTable);
                if (namespaceMappings != null)
                {
                    foreach (KeyValuePair<string, string> mapping in namespaceMappings)
                    {
                        nsMgr.AddNamespace(mapping.Key, mapping.Value);
                    }
                }

                var results = ((IEnumerable)doc.XPathEvaluate(xpath, nsMgr)).Cast<QueryResultType>();

                if (results.Count() == 0)
                {
                    throw new InvalidOperationException("No matches for " + xpath);
                }

                foreach (QueryResultType result in results.Cast<QueryResultType>())
                {
                    action(result, data);
                }

                return doc.ToString();
            }
        }

        public static void Serialize(object obj, Stream xamlStream)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(xamlStream, new XmlWriterSettings { Indent = true }))
            {
                //ActivityUtilities.ReplaceLambdaValuesInActivityTree(obj, false, true);
                XamlServices.Save(xmlWriter, obj);
            }
        }

        public static string Serialize(object obj)
        {
            using (MemoryStream xamlStream = new MemoryStream())
            {
                XamlTestDriver.Serialize(obj, xamlStream);
                xamlStream.Position = 0;
                return XamlTestDriver.GetStringFromMemoryStream(xamlStream);
            }
        }

        public static object Deserialize(string xamlString)
        {
            if (xamlString == null)
            {
                throw new ArgumentNullException("xamlString");
            }

            return XamlTestDriver.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(xamlString)));
        }

        public static object Deserialize(Stream xamlStream)
        {
            if (xamlStream == null)
            {
                throw new ArgumentNullException("xamlStream");
            }

            using (XmlReader reader = XmlReader.Create(xamlStream))
            {
                return XamlTestDriver.Deserialize(reader);
            }
        }

        public static object Deserialize(XmlReader xamlReader)
        {
            if (xamlReader == null)
            {
                throw new ArgumentNullException("xamlReader");
            }

            // This is a hack to load the System.Activities.Core dll into 
            // the appdomain
            Sequence sequence = new Sequence();

            // This needs to be uncommented when xaml deserialization can be used in full trust
            return XamlServices.Load(xamlReader);
            //return serializer.Load(xamlReader);
            //return PartialTrustCaller.Deserialize(xamlReader);
        }

        public static object RoundTrip(object obj)
        {
            MemoryStream xamlStream = null;
            try
            {
                return RoundTrip(obj, out xamlStream);
            }
            finally
            {
                if (xamlStream != null)
                {
                    xamlStream.Close();
                }
            }
        }

        // caller to close the output stream which contains XAML.
        static object RoundTrip(object obj, out MemoryStream xamlStream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            xamlStream = new MemoryStream();

            try
            {
                Serialize(obj, xamlStream);

                xamlStream.Position = 0;

                // Create Xaml subdirectory
                //string xamlSubDirectory = DirectoryAssistance.GetArtifactDirectory("Xaml");
                string xamlSubDirectory = Path.GetTempPath();
                if (!Directory.Exists(xamlSubDirectory))
                {
                    Directory.CreateDirectory(xamlSubDirectory);
                }

                //string fileName = DirectoryAssistance.GetTempFileWithGuid("Xaml\\XamlRoundtrip_{0}.xaml");
                string fileName = Path.Combine(Path.GetTempPath(), $"XamlRoundtrip_{Guid.NewGuid().ToString()}.xaml");
                //Log.TraceInternal("Saving xaml to {0}.", fileName);
                //Log.TraceInternal("For official lab runs, the file will also be available on the file tab.");
                File.WriteAllText(fileName, GetStringFromMemoryStream(xamlStream));

                xamlStream.Position = 0;
                using (XmlReader reader = XmlReader.Create(xamlStream))
                {
                    return Deserialize(reader);
                }
            }
            finally
            {
                xamlStream.Position = 0;
            }
        }

        static void TraceXamlFile(MemoryStream xamlStream)
        {
            XamlTestDriver.TraceXamlFile(XamlTestDriver.GetStringFromMemoryStream(xamlStream));
            xamlStream.Position = 0;
        }

        static void TraceXamlFile(string xaml)
        {
            //Log.TraceInternal(xaml);
        }

        static string GetStringFromMemoryStream(MemoryStream xamlStream)
        {
            //Not closing the reader, otherwise the underlying memory stream will be closed. It will be closed by the caller
            xamlStream.Position = 0;
            StreamReader reader = new StreamReader(xamlStream);
            return reader.ReadToEnd();
        }
        public delegate string ModifyXaml(string xaml);
    }
}
