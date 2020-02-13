// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace TestObjects.Xaml.GraphCore
{
    public delegate void GraphNodeOperationDelegate(IGraphNode node, object state);
    public delegate IGraphNode TransformOperation(IGraphNode node, object state);


    [Serializable]
    public abstract class GraphNode : TestDependencyObject, IGraphNode
    {
        private IList<IGraphNode> children;
        private IGraphNode parent = null;
        private string name;

        public GraphNode()
            : this("NoName", null)
        {
        }

        public GraphNode(string name)
            : this(name, null)
        {
        }

        public GraphNode(string name, IGraphNode parent)
        {
            this.Name = name;
            this.Parent = parent;
            this.children = new List<IGraphNode>();
        }
        public IList<IGraphNode> Children
        {
            get { return this.children; }
        }
        public IGraphNode Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public int Depth
        {
            get
            {
                // Walk until root and give the depth //
                IGraphNode node = this;
                int depth = 0;
                while (node.Parent != null)
                {
                    depth++;
                    node = node.Parent;
                }
                return depth;
            }
        }

        public string QualifiedName
        {
            get
            {
                // Trace until root and generate a qualified name //
                IGraphNode node = this;
                string name = this.Name;
                while (node.Parent != null)
                {
                    name = node.Parent.Name + "_" + name;
                    node = node.Parent;
                }
                return name;
            }
        }


        /// <summary>
        ///  Gets list of all nodes that are reachable through
        ///  the children links - inclides this node as well
        /// </summary>
        public List<IGraphNode> Decendants
        {
            get
            {
                List<IGraphNode> visited = new List<IGraphNode>();
                this.BreadthFirstSearchOperation(new GraphNodeOperationDelegate(visitNode), visited);

                return visited;
            }
        }

        private static void visitNode(IGraphNode node, object state)
        {
            List<IGraphNode> visited = state as List<IGraphNode>;
            if (visited.Contains(node))
            {
                return;
            }
            else
            {
                visited.Add(node);
            }
        }

        #region Graph Operations

        public IGraphNode Transform(TransformOperation operation, object state)
        {
            Stack<TransformWalkInfo> frames = new Stack<TransformWalkInfo>();
            TransformWalkInfo rootInfo = new TransformWalkInfo { CurrentNode = this, TransformedNode = operation(this, state) };
            frames.Push(rootInfo);
            IGraphNode root = rootInfo.TransformedNode;
            Dictionary<IGraphNode, IGraphNode> visitedNodes = new Dictionary<IGraphNode, IGraphNode>();


            while (frames.Count > 0)
            {
                TransformWalkInfo frame = frames.Pop();

                if (visitedNodes.ContainsKey(frame.CurrentNode))
                {
                    continue;
                }
                else
                {
                    visitedNodes[frame.CurrentNode] = frame.TransformedNode;
                }

                for (int i = 0; i < frame.CurrentNode.Children.Count; i++)
                {
                    IGraphNode current = frame.CurrentNode.Children[i];
                    IGraphNode transformed = operation(current, state);
                    frame.TransformedNode.Children.Add(transformed);
                    frames.Push(new TransformWalkInfo { CurrentNode = current, TransformedNode = transformed });
                }
            }

            return root;
        }

        public void DepthFirstSearchOperation(GraphNodeOperationDelegate operation, object state)
        {
            DepthFirstSearchOperation(operation, null, state);
        }

        public void DepthFirstSearchOperation(GraphNodeOperationDelegate preVisit, GraphNodeOperationDelegate postVisit, object state)
        {
            // nothing happens unless one operation is provided
            if (preVisit == null && postVisit == null)
            {
                return;
            }

            // we must be able to distinguish these for the algorithm below to work
            if (preVisit == postVisit)
            {
                throw new InvalidOperationException("Operations must be different.");
            }

            List<IGraphNode> visitedNodes = new List<IGraphNode>();
            Stack<DepthFirstWalkInfo> frames = new Stack<DepthFirstWalkInfo>();
            frames.Push(new DepthFirstWalkInfo { CurrentNode = this, Operation = preVisit });

            while (frames.Count > 0)
            {
                DepthFirstWalkInfo current = frames.Pop();

                if (visitedNodes.Contains(current.CurrentNode))
                {
                    if (current.Operation == preVisit)
                    {
                        continue;
                    }
                }
                else
                {
                    visitedNodes.Add(current.CurrentNode);
                }


                if (current.Operation != null)
                {
                    current.Operation(current.CurrentNode, state);
                }

                if (current.Operation != postVisit)
                {
                    frames.Push(new DepthFirstWalkInfo { CurrentNode = current.CurrentNode, Operation = postVisit });

                    // this goes into the stack in reverse order of what we need, 
                    // put onto one stack then another to reverse it
                    Stack<DepthFirstWalkInfo> tempStack = new Stack<DepthFirstWalkInfo>();
                    foreach (IGraphNode child in current.CurrentNode.Children)
                    {
                        tempStack.Push(new DepthFirstWalkInfo { CurrentNode = child, Operation = preVisit });
                    }

                    while (tempStack.Count > 0)
                    {
                        frames.Push(tempStack.Pop());
                    }
                }
            }
        }



        // <summary>
        // Walk the graph like it was a tree from the root - if you encounter a cycle, stop further walking
        // and continue with the next node
        // </summary>
        // <param name="opeartion"></param>
        // <param name="state"></param>
        public void BreadthFirstSearchOperation(GraphNodeOperationDelegate operation, object state)
        {
            Queue<IGraphNode> pendingNodes = new Queue<IGraphNode>();
            pendingNodes.Enqueue(this);
            List<IGraphNode> visitedNodes = new List<IGraphNode>();

            while (pendingNodes.Count != 0)
            {
                IGraphNode node = pendingNodes.Dequeue();
                if (visitedNodes.Contains(node))
                {
                    // cycle - already seen this node //
                    continue;
                }
                else
                {
                    visitedNodes.Add(node);
                }

                operation(node, state);

                foreach (IGraphNode child in node.Children)
                {
                    pendingNodes.Enqueue(child);
                }
            }
        }

        #endregion

        /// <summary>
        /// Walk the graph like it was a tree from the root - if you encounter a cycle, stop further walking
        /// and continue with the next node
        /// Perform an operation on each node, the final result being a new tree
        /// </summary>
        /// <param name="operation">
        /// operation should return a new reference (don't just return the node passed in)
        /// with an empty Children collection
        /// </param>
        /// <param name="state"></param>
        ///

        class TransformWalkInfo
        {
            public IGraphNode CurrentNode { get; set; }
            public IGraphNode TransformedNode { get; set; }
        }

        /// <summary>
        /// Walk the graph like it was a tree from the root - if you encounter a cycle, stop further walking
        /// and continue with the next node
        /// </summary>
        /// <param name="opeartion"></param>
        /// <param name="state"></param>
        ///

        class DepthFirstWalkInfo
        {
            public IGraphNode CurrentNode { get; set; }
            public GraphNodeOperationDelegate Operation { get; set; }
        }
    }
}

