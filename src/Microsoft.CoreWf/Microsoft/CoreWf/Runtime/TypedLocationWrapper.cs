// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
    [DataContract]
    // Users of this class need to be VERY careful because TypedLocationWrapper
    // will happily wrap an inner location of any type.  This, however, could
    // cause an issue when attempting to get or set the value unless the inner
    // location's Type matches exactly.  If the use of the wrapper will be
    // constrained to either get or set then non-matching (but compatible) types
    // can be used.  One example of this is when wrapping a location for use
    // with an out argument.  Since out arguments buffer reads from their own
    // location, we know that only set will be called on this underlying
    // wrapper.
    internal class TypedLocationWrapper<T> : Location<T>
    {
        private Location _innerLocation;

        public TypedLocationWrapper(Location innerLocation)
            : base()
        {
            _innerLocation = innerLocation;
        }

        internal override bool CanBeMapped
        {
            get
            {
                return _innerLocation.CanBeMapped;
            }
        }

        public override T Value
        {
            get
            {
                return (T)_innerLocation.Value;
            }
            set
            {
                _innerLocation.Value = value;
            }
        }

        [DataMember(Name = "innerLocation")]
        internal Location SerializedInnerLocation
        {
            get { return _innerLocation; }
            set { _innerLocation = value; }
        }

        public override string ToString()
        {
            return _innerLocation.ToString();
        }
    }
}
