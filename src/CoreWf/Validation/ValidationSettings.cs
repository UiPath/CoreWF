// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Validation
{
    using System.Activities.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [Fx.Tag.XamlVisible(false)]
    public class ValidationSettings
    {
        private IDictionary<Type, IList<Constraint>> additionalConstraints;

        public CancellationToken CancellationToken
        {
            get;
            set;
        }

        public bool SingleLevel
        {
            get;
            set;
        }

        public bool SkipValidatingRootConfiguration
        {
            get;
            set;
        }

        public bool OnlyUseAdditionalConstraints
        {
            get;
            set;
        }

        public bool PrepareForRuntime
        {
            get;
            set;
        }

        public LocationReferenceEnvironment Environment
        {
            get;
            set;
        }

        internal bool HasAdditionalConstraints
        {
            get
            {
                return this.additionalConstraints != null && this.additionalConstraints.Count > 0;
            }
        }
        
        public IDictionary<Type, IList<Constraint>> AdditionalConstraints
        {
            get
            {
                if (this.additionalConstraints == null)
                {
                    this.additionalConstraints = new Dictionary<Type, IList<Constraint>>(); 
                }

                return this.additionalConstraints;
            }
        }        
    }
}
