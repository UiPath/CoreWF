// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;
using System.Globalization;

namespace System.Activities.Runtime;

[DataContract]
internal class MappableObjectManager
{
    private List<MappableLocation> _mappableLocations;

    public MappableObjectManager() { }

    public int Count
    {
        get
        {
            int result = 0;
            if (_mappableLocations != null)
            {
                result += _mappableLocations.Count;
            }

            return result;
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "mappableLocations")]
    internal List<MappableLocation> SerializedMappableLocations
    {
        get => _mappableLocations;
        set => _mappableLocations = value;
    }

    public IDictionary<string, LocationInfo> GatherMappableVariables()
    {
        Dictionary<string, LocationInfo> result = null;
        if (_mappableLocations != null && _mappableLocations.Count > 0)
        {
            result = new Dictionary<string, LocationInfo>(_mappableLocations.Count);
            for (int locationIndex = 0; locationIndex < _mappableLocations.Count; locationIndex++)
            {
                MappableLocation mappableLocation = _mappableLocations[locationIndex];
                result.Add(mappableLocation.MappingKeyName, new LocationInfo(mappableLocation.Name, mappableLocation.OwnerDisplayName, mappableLocation.Location.Value));
            }
        }

        return result;
    }

    public void Register(Location location, Activity activity, LocationReference locationOwner, ActivityInstance activityInstance)
    {
        Fx.Assert(location.CanBeMapped, "should only register mappable locations");

        _mappableLocations ??= new List<MappableLocation>();
        _mappableLocations.Add(new MappableLocation(locationOwner, activity, activityInstance, location));
    }

    public void Unregister(Location location)
    {
        Fx.Assert(location.CanBeMapped, "should only register mappable locations");

        int mappedLocationsCount = _mappableLocations.Count;
        for (int i = 0; i < mappedLocationsCount; i++)
        {
            if (ReferenceEquals(_mappableLocations[i].Location, location))
            {
                _mappableLocations.RemoveAt(i);
                break;
            }
        }
        Fx.Assert(_mappableLocations.Count == mappedLocationsCount - 1, "can only unregister locations that have been registered");
    }

    [DataContract]
    internal class MappableLocation
    {
        private string _mappingKeyName;
        private string _name;
        private string _ownerDisplayName;
        private Location _location;

        public MappableLocation(LocationReference locationOwner, Activity activity, ActivityInstance activityInstance, Location location)
        {
            Name = locationOwner.Name;
            OwnerDisplayName = activity.DisplayName;
            Location = location;
            MappingKeyName = string.Format(CultureInfo.InvariantCulture, "activity.{0}-{1}_{2}", activity.Id, locationOwner.Id, activityInstance.Id);
        }

        internal string MappingKeyName
        {
            get => _mappingKeyName;
            private set => _mappingKeyName = value;
        }

        public string Name
        {
            get => _name;
            private set => _name = value;
        }

        public string OwnerDisplayName
        {
            get => _ownerDisplayName;
            private set => _ownerDisplayName = value;
        }

        internal Location Location
        {
            get => _location;
            private set => _location = value;
        }

        [DataMember(Name = "MappingKeyName")]
        internal string SerializedMappingKeyName
        {
            get => MappingKeyName;
            set => MappingKeyName = value;
        }

        [DataMember(Name = "Name")]
        internal string SerializedName
        {
            get => Name;
            set => Name = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "OwnerDisplayName")]
        internal string SerializedOwnerDisplayName
        {
            get => OwnerDisplayName;
            set => OwnerDisplayName = value;
        }

        [DataMember(Name = "Location")]
        internal Location SerializedLocation
        {
            get => Location;
            set => Location = value;
        }
    }
}
