// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TestObjects.Xaml.GraphCore;

namespace TestObjects.Xaml.GraphOperations.Builders
{
    public static class ObjectGraphWalker
    {
        const string ObjectDataProp = "ObjectData";
        const string ObjectTypeProp = "ObjectType";

        /// <summary>
        /// Build and ObjectGraph following the rules of XAML
        /// - gives back an ObjectGraph which wraps the object
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ObjectGraph Create(object data)
        {
            XamlObjectGraphBuilder builder = new XamlObjectGraphBuilder();
            return (ObjectGraph)ObjectGraphWalker.Create(data, builder);
        }

        public static IGraphNode Create(object data, IXamlObjectGraphBuilder builder)
        {
            return ObjectGraphWalker.BuildGraph("Root", data, data.GetType(), null, builder);
        }

        #region Data used for Build

        public static object GetObjectData(ITestDependencyObject props)
        {
            return props.GetValue(ObjectDataProp);
        }

        public static void SetObjectData(ITestDependencyObject props, object value)
        {
            props.SetValue(ObjectDataProp, value);
        }

        public static void ClearObjectData(ITestDependencyObject props)
        {
            props.Properties.Remove(ObjectDataProp);
        }

        public static Type GetObjectType(ITestDependencyObject props)
        {
            return (Type)props.GetValue(ObjectTypeProp);
        }

        public static void SetObjectType(ITestDependencyObject props, Type value)
        {
            props.SetValue(ObjectTypeProp, value);
        }

        public static void ClearObjectType(ITestDependencyObject props)
        {
            props.Properties.Remove(ObjectTypeProp);
        }
        #endregion
        #region XamlObjectGraphBuilder implementation

        private static IGraphNode BuildGraph(string name, object data, Type type, IGraphNode parent, IXamlObjectGraphBuilder builder)
        {
            Queue<IGraphNode> pendingQueue = new Queue<IGraphNode>();
            Dictionary<int, IGraphNode> visitedObjects = new Dictionary<int, IGraphNode>();

            IGraphNode root = ObjectGraphWalker.BuildNodeHelper(name, data, type, parent, null, builder, false);
            pendingQueue.Enqueue(root);

            while (pendingQueue.Count != 0)
            {
                IGraphNode node = pendingQueue.Dequeue();
                object nodeData = ObjectGraphWalker.GetObjectData(node);
                Type nodeType = ObjectGraphWalker.GetObjectType(node);

                // clear the properties so they don't potentially get serialized
                ObjectGraphWalker.ClearObjectData(node);
                ObjectGraphWalker.ClearObjectType(node);

                if (nodeData == null || nodeType.IsPrimitive == true ||
                    nodeType == typeof(System.String))
                {
                    // we have reached a leaf node //
                    continue;
                }

                if (visitedObjects.Keys.Contains(nodeData.GetHashCode()))
                {
                    // Caused by a cycle - alredy seen this node //
                    IGraphNode builtNode = visitedObjects[nodeData.GetHashCode()];

                    foreach (IGraphNode newChild in builder.BuildVisitedNode(builtNode, node))
                    {
                        node.Children.Add(newChild);
                    }
                    //node.Children.Add(visitedObjects[nodeData.GetHashCode()]);
                    continue;
                }
                // ok the type is a complex type - query all properties //
                // create children for clr properties //
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(nodeData);

                foreach (PropertyDescriptor property in properties)
                {
                    if (ObjectGraphWalker.IsReadablePropertyDescriptor(property))
                    {
                        object value = null;
                        try
                        {
                            value = property.GetValue(nodeData);
                        }
                        catch (Exception ex) // jasonv - approved; convert exception into value
                        {
                            value = ex;
                        }

                        IGraphNode childNode = ObjectGraphWalker.BuildNodeHelper(property.Name, value, property.PropertyType, node, property.Name, builder, property.IsReadOnly);

                        if (childNode == null)
                        {
                            continue;
                        }

                        IGraphNode actualChild = childNode;
                        while (actualChild.Parent != node)
                        {
                            actualChild = actualChild.Parent;
                            if (actualChild == null)
                            {
                                throw new InvalidOperationException("Node returned from BuildNode has invalid parent.");
                            }
                        }

                        node.Children.Add(actualChild);
                        pendingQueue.Enqueue(childNode);
                    }
                }

                // IEnumerable support //
                int count = 0;
                IEnumerable enumerableData = nodeData as IEnumerable;
                if (enumerableData != null && nodeData.GetType() != typeof(System.String))
                {
                    IGraphNode collectionParent = builder.BuildCollectionWrapperNode(node);

                    if (collectionParent != null)
                    {
                        node.Children.Add(collectionParent);
                    }
                    else
                    {
                        collectionParent = node;
                    }

                    IEnumerator enumerator = enumerableData.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        IGraphNode childNode = ObjectGraphWalker.BuildNodeHelper("IEnumerable" + count++, enumerator.Current, enumerator.Current == null ? typeof(object) : enumerator.Current.GetType(), collectionParent, null, builder, false);
                        if (childNode == null)
                        {
                            continue;
                        }
                        collectionParent.Children.Add(childNode);
                        pendingQueue.Enqueue(childNode);
                    }
                }

                visitedObjects.Add(nodeData.GetHashCode(), node);
            }

            return root;

        }

        private static IGraphNode BuildNodeHelper(string name, object data, Type type, IGraphNode parent, string propertyName, IXamlObjectGraphBuilder builder, bool isReadOnly)
        {
            IGraphNode node = builder.BuildNode(name, data, type, parent, propertyName, isReadOnly);
            if (node == null)
            {
                return node;
            }
            ObjectGraphWalker.SetObjectData(node, data);
            ObjectGraphWalker.SetObjectType(node, type);
            return node;
        }

        #endregion

        #region Helpers
        // Checks if GetValue may be called on the given PropertyDescriptor.
        private static bool IsReadablePropertyDescriptor(PropertyDescriptor property)
        {
            return !(property.ComponentType is System.Reflection.MemberInfo)
                   || !ObjectGraphWalker.IsGenericTypeMember(property.ComponentType, property.Name);
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
        #endregion
    }
}
