// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System.Collections.Generic;
    using CoreWf;

    public abstract class FlowNode
    {
        private Flowchart owner;
        private int cacheId;

        internal FlowNode()
        {
            Index = -1;
        }

        internal abstract Activity ChildActivity
        {
            get;
        }

        internal int Index
        { 
            get; 
            set; 
        }

        internal bool IsOpen
        {
            get
            {
                return this.owner != null;
            }
        }
        
        internal Flowchart Owner
        {
            get
            {
                return this.owner;
            }
        }

        // Returns true if this is the first time we've visited this node during this pass
        internal bool Open(Flowchart owner, NativeActivityMetadata metadata)
        {
            if (this.cacheId == owner.CacheId)
            {
                // We've already visited this node during this pass
                if (!object.ReferenceEquals(this.owner, owner))
                {
                    metadata.AddValidationError(SR.FlowNodeCannotBeShared(this.owner.DisplayName, owner.DisplayName));
                }

                // Whether we found an issue or not we don't want to change
                // the metadata during this pass.
                return false;
            }

            // if owner.ValidateUnconnectedNodes - Flowchart will be responsible for calling OnOpen for all the Nodes (connected and unconnected)
            if (!owner.ValidateUnconnectedNodes)
            {
                OnOpen(owner, metadata);
            }
            this.owner = owner;
            this.cacheId = owner.CacheId;
            this.Index = -1;

            return true;
        }

        internal abstract void OnOpen(Flowchart owner, NativeActivityMetadata metadata);
        
        internal void GetChildActivities(ICollection<Activity> children)
        {
            if (ChildActivity != null)
            {
                children.Add(ChildActivity);
            }
        }

        internal abstract void GetConnectedNodes(IList<FlowNode> connections);
    }
}
