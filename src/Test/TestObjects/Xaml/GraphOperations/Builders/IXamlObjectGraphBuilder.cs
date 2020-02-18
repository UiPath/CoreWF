// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using TestObjects.Xaml.GraphCore;

namespace TestObjects.Xaml.GraphOperations.Builders
{
    public interface IXamlObjectGraphBuilder
    {
        IGraphNode BuildNode(string name, object data, Type type, IGraphNode parent, string propertyName, bool isReadOnly);
        IList<IGraphNode> BuildVisitedNode(IGraphNode builtNode, IGraphNode parent);
        IGraphNode BuildCollectionWrapperNode(IGraphNode parent);
    }
}
