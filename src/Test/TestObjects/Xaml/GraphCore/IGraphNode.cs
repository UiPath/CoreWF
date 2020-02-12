using System.Collections.Generic;

namespace TestObjects.Xaml.GraphCore
{
    public interface IGraphNode : ITestDependencyObject
    {
        IList<IGraphNode> Children { get; }
        IGraphNode Parent { get; set; }

        string Name { get; set; }
        string QualifiedName { get; }

        void DepthFirstSearchOperation(GraphNodeOperationDelegate operation, object state);
        void DepthFirstSearchOperation(GraphNodeOperationDelegate preVisit, GraphNodeOperationDelegate postVisit, object state);
        void BreadthFirstSearchOperation(GraphNodeOperationDelegate operation, object state);
        IGraphNode Transform(TransformOperation operation, object state);
    }
}
