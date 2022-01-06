// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

// Users of this class need to be VERY careful because TypedLocationWrapper
// will happily wrap an inner location of any type.  This, however, could
// cause an issue when attempting to get or set the value unless the inner
// location's Type matches exactly.  If the use of the wrapper will be
// constrained to either get or set then non-matching (but compatible) types
// can be used.  One example of this is when wrapping a location for use
// with an out argument.  Since out arguments buffer reads from their own
// location, we know that only set will be called on this underlying
// wrapper.
[DataContract]
internal class TypedLocationWrapper<T> : Location<T>
{
    private Location _innerLocation;

    public TypedLocationWrapper(Location innerLocation)
        : base()
    {
        _innerLocation = innerLocation;
    }

    internal override bool CanBeMapped => _innerLocation.CanBeMapped;

    public override T Value
    {
        get => (T)_innerLocation.Value;
        set => _innerLocation.Value = value;
    }

    [DataMember(Name = "innerLocation")]
    internal Location SerializedInnerLocation
    {
        get => _innerLocation;
        set => _innerLocation = value;
    }

    public override string ToString() => _innerLocation.ToString();
}
