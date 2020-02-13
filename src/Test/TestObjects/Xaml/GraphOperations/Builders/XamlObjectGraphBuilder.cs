// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using TestObjects.Xaml.GraphCore;

namespace TestObjects.Xaml.GraphOperations.Builders
{
    public class XamlObjectGraphBuilder : IXamlObjectGraphBuilder
    {
        public XamlObjectGraphBuilder()
        {

        }

        #region IXamlObjectGraphBuilder Members

        public IGraphNode BuildNode(string name, object data, Type type, IGraphNode parent, string propertyName, bool isReadOnly)
        {
            return new ObjectGraph(name, data, type, parent);
        }

        public IList<IGraphNode> BuildVisitedNode(IGraphNode builtNode, IGraphNode parent)
        {
            return new List<IGraphNode>
            {
                builtNode
            };
        }

        public IGraphNode BuildCollectionWrapperNode(IGraphNode parent)
        {
            // no collection wrapper
            return null;
        }

        #endregion
    }
}
