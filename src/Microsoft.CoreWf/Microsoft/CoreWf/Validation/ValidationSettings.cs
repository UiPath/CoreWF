// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CoreWf.Validation
{
    [Fx.Tag.XamlVisible(false)]
    public class ValidationSettings
    {
        private IDictionary<Type, IList<Constraint>> _additionalConstraints;

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
                return _additionalConstraints != null && _additionalConstraints.Count > 0;
            }
        }

        public IDictionary<Type, IList<Constraint>> AdditionalConstraints
        {
            get
            {
                if (_additionalConstraints == null)
                {
                    _additionalConstraints = new Dictionary<Type, IList<Constraint>>();
                }

                return _additionalConstraints;
            }
        }
    }
}
