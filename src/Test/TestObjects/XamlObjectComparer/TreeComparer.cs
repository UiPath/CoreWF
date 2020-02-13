using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;
using TestObjects.Xaml.GraphOperations.Comparers;

namespace TestObjects.XamlObjectComparer
{
    // <summary>
    // A class providing a static method to compare two objects
    // by comparing node by node and property by property.
    // </summary>
    public static class TreeComparer
    {

        // <summary>
        // </summary>
        static public bool BreakOnError = false;

        private static Dictionary<string, PropertyToIgnore> _skipProperties = null;
        private static Dictionary<string, PropertyToIgnore> _skipPropertiesDefault = ReadSkipProperties();
        private static List<int>[] _objectsInTree = new List<int>[2];
        // <summary>
        // Compare two object trees. If all the descendant logical nodes 
        // are equivalent, return true, otherwise, return false.
        // </summary>
        // <param name="firstTree">The root for the first tree</param>
        // <param name="secondTree">The root for the second tree</param>
        // <remarks>
        // Compares every event and property for each the node.
        // </remarks>
        // <returns>
        // A structure containing result. If the returned variable name is result.
        // result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        // and CompareResult.Different otherwise.
        // </returns>
        public static TreeComparerResult CompareLogical(
            Object firstTree,
            Object secondTree)
        {
            TreeComparerResult result = TreeComparer.CompareLogical(firstTree, secondTree, TreeComparer._skipPropertiesDefault);
            ObjectGraphComparer.XamlCompareParity(firstTree, secondTree, result.Result);

            return result;
        }
        // <summary>
        // Compare two object trees. If all the descendant logical nodes 
        // are equivalent, return true, otherwise, return false.
        // </summary>
        // <param name="firstTree">The root for the first tree.</param>
        // <param name="secondTree">The root for the second tree.</param>
        // <param name="fileName">Custom list of properties to ignore.</param>
        // <remarks>
        // Compares every event and property for each the node.
        // </remarks>
        // <returns>
        // A structure containing result. If the returned variable name is result.
        // result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        // and CompareResult.Different otherwise.
        // </returns>
        private static TreeComparerResult CompareLogical(
            Object firstTree,
            Object secondTree,
            string fileName)
        {
            Dictionary<string, PropertyToIgnore> props = TreeComparer.ReadSkipProperties(fileName);

            return TreeComparer.CompareLogical(firstTree, secondTree, props);
        }
        // <summary>
        // Compare two object trees. If all the descendant logical nodes 
        // are equivalent, return true, otherwise, return false.
        // </summary>
        // <param name="firstTree">The root for the first tree.</param>
        // <param name="secondTree">The root for the second tree.</param>
        // <param name="propertiesToIgnore">Custom list of properties to ignore.</param>
        // <remarks>
        // Compares every event and property for each the node.
        // </remarks>
        // <returns>
        // A structure containing result. If the returned variable name is result.
        // result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        // and CompareResult.Different otherwise.
        // </returns>
        public static TreeComparerResult CompareLogical(
            Object firstTree,
            Object secondTree,
            Dictionary<string, PropertyToIgnore> propertiesToIgnore)
        {
            if (propertiesToIgnore == null)
            {
                throw new ArgumentNullException("propertiesToIgnore", "Argument must be a non-null Dictionary.");
            }

            TreeComparerResult result = new TreeComparerResult();

            result.Result = CompareResult.Equivalent;

            // Validate parameters, both objects are null
            if (null == firstTree && null == secondTree)
            {
                return result;
            }

            result.Result = CompareResult.Different;

            // Validate parameters, only one object is null
            if (null == firstTree || null == secondTree)
            {
                return result;
            }

            // Compare the types 
            if (!firstTree.GetType().Equals(secondTree.GetType()))
            {
                TreeComparer.SendCompareMessage("Two nodes have different types: '" + firstTree.GetType().FullName + "' vs. '" + secondTree.GetType().FullName + "'.");
                TreeComparer.Break();
                return result;
            }

            bool same = false;
            //lock (TreeComparer._objectsInTree)
            //{
                // Create hashtables that will contain objects in the trees.
                // This is used to break loops.
                TreeComparer._objectsInTree[0] = new List<int>();
                TreeComparer._objectsInTree[1] = new List<int>();

                TreeComparer._skipProperties = propertiesToIgnore;

                // Include default skip properties if necessary.
                if (TreeComparer._skipProperties != TreeComparer._skipPropertiesDefault)
                {
                    TreeComparer._MergeDictionaries(TreeComparer._skipProperties, TreeComparer._skipPropertiesDefault);
                }

                try
                {
                    same = CompareObjects(firstTree, secondTree);
                }
                finally
                {
                    _objectsInTree[0] = null;
                    _objectsInTree[1] = null;
                    _skipProperties = null;
                }
            //}

            // Two trees are equivalent
            if (same)
            {
                result.Result = CompareResult.Equivalent;
            }

            return result;
        }

        // <summary>
        // Recursively compare two object trees following logical tree structure
        // </summary>
        // <param name="firstTree">Root for first tree</param>
        // <param name="secondTree">Root for second tree</param>
        // <returns>
        //   True, if two object tree are equivalent
        //   False, otherwise
        // </returns>
        private static bool CompareLogicalTree(
            object firstTree,
            object secondTree
            )
        {
            return TreeComparer.CompareObjects(firstTree, secondTree);
        }


        private static int _MergeDictionaries(Dictionary<string, PropertyToIgnore> dictionary1, Dictionary<string, PropertyToIgnore> dictionary2)
        {
            int cnt = 0;

            foreach (string propName in dictionary2.Keys)
            {
                if (!dictionary1.ContainsKey(propName))
                {
                    dictionary1.Add(propName, dictionary2[propName]);
                    cnt++;
                }
            }

            return cnt;
        }

        // <summary>
        // Compare Properties for two nodes. If all the properties for these two
        // nodes have the same value, return true. Otherwise, return false.
        //
        // </summary>
        // <param name="firstNode">The first node</param>
        // <param name="secondNode">The second node</param>
        private static bool CompareObjectProperties(object firstNode, object secondNode)
        {
            //
            // Compare CLR properties.
            //
            Dictionary<string, PropertyDescriptor> clrProperties1 = TreeComparer.GetClrProperties(firstNode);
            Dictionary<string, PropertyDescriptor> clrProperties2 = TreeComparer.GetClrProperties(secondNode);

            if (!TreeComparer.CompareClrPropertyCollection(firstNode, clrProperties1, secondNode, clrProperties2))
            {
                TreeComparer.SendCompareMessage("The first node and the second node are different in one or more CLR properties.");
                TreeComparer.Break();
                return false;
            }

            if (!TreeComparer.ComparePropertyAsIEnumerable(firstNode, secondNode))
            {
                TreeComparer.SendCompareMessage("The first node and the second node are different collections.");
                TreeComparer.Break();
                return false;
            }

            return true;
        }

        // <summary>
        // Compare a collection of clr properties.
        // </summary>
        // <returns>
        // true, if all properties are equivalent
        // false, otherwise
        // </returns>
        private static bool CompareClrPropertyCollection(
            Object firstNode,
            Dictionary<string, PropertyDescriptor> properties1,
            Object secondNode,
            Dictionary<string, PropertyDescriptor> properties2)
        {
            IEnumerator<string> ie1 = properties1.Keys.GetEnumerator();

            while (ie1.MoveNext())
            {
                string propertyName = ie1.Current;

                // Check that the second tree contains the property.
                if (!properties2.ContainsKey(propertyName))
                {
                    TreeComparer.SendCompareMessage("Property '" + propertyName + "' is not in second tree.");
                    TreeComparer.Break();
                    return false;
                }

                // If property was in skip collection, ignore it
                if (!TreeComparer.ShouldIgnoreProperty(propertyName, firstNode, IgnoreProperty.IgnoreValueOnly))
                {

                    // Compare properties
                    if (!TreeComparer.CompareClrProperty(
                            firstNode,
                            properties1[propertyName],
                            secondNode,
                            properties2[propertyName]))
                    {
                        TreeComparer.SendCompareMessage("Value of property '" + propertyName + "' is different.");
                        TreeComparer.Break();
                        return false;
                    }
                }

                properties2.Remove(propertyName);
            }

            // Check that the second tree doesn't have more properties than the first tree.
            if (properties2.Count > 0)
            {
                IEnumerator<string> ie2 = properties2.Keys.GetEnumerator();
                ie2.MoveNext();

                TreeComparer.SendCompareMessage("Property '" + properties2[ie2.Current].Name + "' is not in first tree.");
                TreeComparer.Break();
                return false;
            }

            return true;
        }

        // <summary>
        // Get clr properties
        // </summary>
        // <param name="owner">owner</param>
        private static Dictionary<string, PropertyDescriptor> GetClrProperties(object owner)
        {
            Dictionary<string, PropertyDescriptor> clrProperties = new Dictionary<string, PropertyDescriptor>();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(owner);

            foreach (PropertyDescriptor property in properties)
            {
                // skip properties
                if (TreeComparer.ShouldIgnoreProperty(property.Name, owner, IgnoreProperty.IgnoreNameAndValue))
                {
                    continue;
                }

                clrProperties.Add(property.Name, property);
            }

            return clrProperties;
        }

        // <summary>
        //  Shoud ignore this property?
        // </summary>
        // <param name="propertyName">property name</param>
        // <param name="owner">owner</param>
        // <param name="whatToIgnore">Valueonly or Value and name to ignore?</param>
        // <returns></returns>
        private static bool ShouldIgnoreProperty(string propertyName, object owner, IgnoreProperty whatToIgnore)
        {
            PropertyToIgnore property = null;
            foreach (string key in TreeComparer._skipProperties.Keys)
            {
                if (String.Equals(key, propertyName, StringComparison.InvariantCulture)
                    || key.StartsWith(propertyName + "___owner___"))
                {
                    property = TreeComparer._skipProperties[key];
                    if (whatToIgnore == property.WhatToIgnore && ((null == property.Owner) || TreeComparer._DoesTypeMatch(owner.GetType(), property.Owner)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool _DoesTypeMatch(Type ownerType, string typeName)
        {
            Type type = ownerType;
            bool isMatch = false;

            while (type != null && !isMatch)
            {
                if (0 == String.Compare(type.Name, typeName))
                {
                    isMatch = true;
                }

                type = type.BaseType;
            }

            return isMatch;
        }

        // <summary>
        // Read properties to skip from PropertiesToSkip.xml. If this file
        // exists under the current working directory, use the one there. 
        // Otherwise, use the file built in the ClientTestLibrary Assembly.
        // </summary>
        // <returns>Hashtable containing properties should be skiped</returns>
        private static Dictionary<string, PropertyToIgnore> ReadSkipProperties()
        {
            // File name for the properties to skip.
            return TreeComparer.ReadSkipProperties("PropertiesToSkip.xml");
        }

        // <summary>
        // Read properties to skip from PropertiesToSkip.xml. If this file
        // exists under the current working directory, use the one there. 
        // Otherwise, use the file built in the ClientTestLibrary Assembly.
        // </summary>
        // <param name="fileName">Name of config file for specifying properties.</param>
        // <returns>Hashtable containing properties should be skiped</returns>
        public static Dictionary<string, PropertyToIgnore> ReadSkipProperties(string fileName)
        {
            Dictionary<string, PropertyToIgnore> PropertiesToSkip = new Dictionary<string, PropertyToIgnore>();

            //
            // Load PropertiesToSkip.xml document from assembly resources.
            //
            XmlDocument doc = new XmlDocument();
            Stream xmlFileStream = null;
            if (File.Exists(fileName))
            {
                TreeComparer.SendCompareMessage("Opening '" + fileName + "' from the current PartialTrustDirectory.");
                xmlFileStream = File.OpenRead(fileName);
            }
            else
            {
                TreeComparer.SendCompareMessage("Opening '" + fileName + "' from the Assembly.");
                Assembly asm = Assembly.GetAssembly(typeof(TreeComparer));
                xmlFileStream = asm.GetManifestResourceStream(fileName);

                if (xmlFileStream == null)
                {
                    return PropertiesToSkip;
                }
            }

            try
            {
                StreamReader reader = new StreamReader(xmlFileStream);
                doc.LoadXml(reader.ReadToEnd());
            }
            finally
            {
                xmlFileStream.Close();
            }

            //
            // Store properties to skip in collection.
            //
            XmlNodeList properties = doc.GetElementsByTagName("PropertyToSkip");

            foreach (XmlNode property in properties)
            {
                string propertyName = TreeComparer.GetAttributeValue(property, "PropertyName");
                string ignore = TreeComparer.GetAttributeValue(property, "Ignore");
                string owner = TreeComparer.GetAttributeValue(property, "Owner");

                IgnoreProperty whatToIgnore;

                if (null == ignore || 0 == String.Compare(ignore, "ValueOnly"))
                {
                    whatToIgnore = IgnoreProperty.IgnoreValueOnly;
                }
                else if (0 == String.Compare(ignore, "NameAndValue"))
                {
                    whatToIgnore = IgnoreProperty.IgnoreNameAndValue;
                }
                else
                {
                    throw new Exception("'Ignore' attribute value not recognized: " + ignore);
                }

                PropertyToIgnore newItem = new PropertyToIgnore();

                newItem.WhatToIgnore = whatToIgnore;

                if (!String.IsNullOrEmpty(owner))
                {
                    newItem.Owner = owner;
                }

                if (PropertiesToSkip.ContainsKey(propertyName))
                {
                    SendCompareMessage(propertyName);
                }
                PropertiesToSkip.Add(propertyName + "___owner___" + owner, newItem);
            }

            return PropertiesToSkip;
        }

        private static string GetAttributeValue(
            XmlNode node,
            string attributeName)
        {
            XmlAttributeCollection attributes = node.Attributes;
            XmlAttribute attribute = attributes[attributeName];

            if (null == attribute)
            {
                return null;
            }

            return attribute.Value;
        }

        // Checks if GetValue may be called on the given PropertyDescriptor.
        private static bool IsReadablePropertyDescriptor(PropertyDescriptor property)
        {
            return !(property.ComponentType is System.Reflection.MemberInfo)
                   && !TreeComparer.IsGenericTypeMember(property.ComponentType, property.Name);
        }

        // Checks if the given type member is a generic-only member on a non-generic type.
        private static bool IsGenericTypeMember(Type type, string memberName)
        {
            return !type.IsGenericType
                    && (memberName == "GenericParameterPosition"
                    || memberName == "GenericParameterAttributes"
                    || memberName == "GetGenericArguments"
                    || memberName == "GetGenericParameterConstraints"
                    || memberName == "GetGenericTypeDefinition"
                    || memberName == "IsGenericTypeDefinition"
                    || memberName == "DeclaringMethod");
        }

        // <summary>
        // Compare two clr properties.
        // </summary>
        // <returns>
        // true, if they are the same
        // false, otherwise
        // </returns>
        //Justification: Safe to assert permission because it does not call our produce code from the using statement and code inspection
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static bool CompareClrProperty(
                object owner1,
                PropertyDescriptor property1,
                object owner2,
                PropertyDescriptor property2)
        {
            //both are simple property, convert them into string and compare
            object obj1;
            object obj2;
            //Show property to be compared.
            //SendCompareMessage("Compare Clr property '" + property1.Name + " owner: " + owner1.GetType().Name);

            if (IsReadablePropertyDescriptor(property1))
            {
                // In the case of an exception when accessing the property,
                // we validate that the same exception is thrown in both cases
                // by replacing the properties value with the exception.
                try
                {
                    obj1 = property1.GetValue(owner1);
                }
                catch (System.Reflection.TargetInvocationException e) 
                {
                    obj1 = e.InnerException;
                }

                try
                {
                    obj2 = property2.GetValue(owner2);
                }
                catch (System.Reflection.TargetInvocationException e) 
                {
                    obj2 = e.InnerException;
                }

                bool same = CompareObjects(obj1, obj2);

                if (!same)
                {
                    SendCompareMessage("Clr property '" + property1.Name + "' is different.");
                    Break();
                }

                return same;
            }
            else
            {
                return true;
            }
        }

        // <summary>
        // Compare the value for of a property of LogicalTreeNode.
        // If the value are not of the same type, return false.
        // if the value can be convert to string and the result is not
        // the same just return false.
        // For logical tree nodes, call CompareLogicalTree to compare recursively.
        // Otherwise, use CompareAsGenericObject
        // to compare
        // </summary>
        // <param name="obj1">The first value</param>
        // <param name="obj2">The second value</param>
        // <returns>
        // true, if value is regarded as the same
        // false, otherwise use CompareAsGenericObject to compare
        // </returns>
        private static bool CompareObjects(object obj1, object obj2)
        {
            bool same = false;

            //Both are null
            if (null == obj1 && null == obj2)
            {
                return true;
            }

            //Only one of them is null
            if (null == obj1)
            {

                TreeComparer.SendCompareMessage("Values is different: 'null' vs. '" + obj2.ToString() + "'.");
                TreeComparer.Break();
                return false;
            }

            if (null == obj2)
            {

                TreeComparer.SendCompareMessage("Values are different: '" + obj1.ToString() + "' vs. 'null'.");
                TreeComparer.Break();
                return false;
            }

            //Compare Type
            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();

            if (!type1.Equals(type2))
            {

                TreeComparer.SendCompareMessage("Type of value is different: '" + type1.FullName + "' vs. '" + type2.FullName + "'.");
                TreeComparer.Break();
                return false;
            }


            if (type1.IsPrimitive)
            {
                same = TreeComparer.ComparePrimitive(obj1, obj2);
                return same;
            }

            if (TreeComparer._objectsInTree[0].Contains(obj1.GetHashCode()) || TreeComparer._objectsInTree[1].Contains(obj2.GetHashCode()))
            {
                return true;
            }

            TreeComparer._objectsInTree[0].Add(obj1.GetHashCode());
            TreeComparer._objectsInTree[1].Add(obj2.GetHashCode());

            return TreeComparer.CompareGenericObject(obj1, obj2); ;
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

            // for double or float comparison, certain error should be allowed. 
            if (obj1 is double)
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

            if (!same)
            {
                TreeComparer.SendCompareMessage("Values are different: '" + obj1.ToString() + "' vs. '" + obj2.ToString() + "'.");
                TreeComparer.Break();
            }

            return same;
        }

        // For a generic object value, just compare their properties.
        private static bool CompareGenericObject(object obj1, object obj2)
        {
            //Compare properties
            if (!TreeComparer.CompareObjectProperties(obj1, obj2))
            {
                TreeComparer.SendCompareMessage("Not all the properties are the same for object '" + obj1.GetType().ToString() + "'.");
                TreeComparer.Break();
                return false;
            }

            return true;
        }

        // <summary>
        // </summary>
        private static void Break()
        {
            if (TreeComparer.BreakOnError)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        // <summary>
        // Compare collections of properties
        // </summary>
        // <param name="properties1">The first property that is collection</param>
        // <param name="properties2">The second property that is collection</param>
        // <returns>
        // true, if they are the same
        // false, otherwise
        // </returns>
        private static bool ComparePropertyAsIEnumerable(
            object properties1,
            object properties2)
        {
            IEnumerable firstEnumerable = properties1 as IEnumerable;
            IEnumerable secondEnumerable = properties2 as IEnumerable;

            if (firstEnumerable == null && secondEnumerable == null)
            {
                return true;
            }

            if (firstEnumerable == null)
            {
                TreeComparer.SendCompareMessage("properties1 is not IEnumerable");
                TreeComparer.Break();
                return false;
            }

            if (secondEnumerable == null)
            {
                TreeComparer.SendCompareMessage("properties2 is not IEnumerable");
                TreeComparer.Break();
                return false;
            }

            IEnumerator firstEnumerator = firstEnumerable.GetEnumerator();
            IEnumerator secondEnumerator = secondEnumerable.GetEnumerator();
            uint firstNodeCount = 0;
            uint secondNodeCount = 0;

            while (firstEnumerator.MoveNext())
            {
                firstNodeCount++;
                if (!secondEnumerator.MoveNext())
                {
                    break;
                }

                secondNodeCount++;

                if (!TreeComparer.CompareGenericObject(firstEnumerator.Current, secondEnumerator.Current))
                {
                    TreeComparer.SendCompareMessage("The first node and the second node have different values in collection");
                    TreeComparer.Break();
                    return false;
                }
            }

            if (secondEnumerator.MoveNext())
            {
                secondNodeCount++;
            }

            if (firstNodeCount != secondNodeCount)
            {
                TreeComparer.SendCompareMessage("The first node and the second node have different lengths");
                TreeComparer.Break();
                return false;
            }

            return true;
        }

        // <summary>
        // Logging
        // </summary>
        private static void SendCompareMessage(string message)
        {
            // Log to console temporarily
            //Log.TraceInternal(message);
            // TODO: Log to xUnit?
        }
    }
}
