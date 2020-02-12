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
