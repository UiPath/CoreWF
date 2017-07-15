// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    internal static class InstancePersistence
    {
        private static readonly XNamespace s_activitiesCommandNamespace = XNamespace.Get("urn:schemas-microsoft-com:CoreWf.Persistence/command");
        private static readonly XNamespace s_activitiesEventNamespace = XNamespace.Get("urn:schemas-microsoft-com:CoreWf.Persistence/event");

        public static XNamespace ActivitiesCommandNamespace
        {
            get
            {
                return InstancePersistence.s_activitiesCommandNamespace;
            }
        }

        public static XNamespace ActivitiesEventNamespace
        {
            get
            {
                return InstancePersistence.s_activitiesEventNamespace;
            }
        }

        public static void ValidatePropertyBag(this IDictionary<XName, InstanceValue> bag)
        {
            bag.ValidatePropertyBag(false);
        }

        public static void ValidatePropertyBag(this IDictionary<XName, InstanceValue> bag, bool allowDelete)
        {
            if (bag != null)
            {
                foreach (KeyValuePair<XName, InstanceValue> property in bag)
                {
                    property.ValidateProperty(allowDelete);
                }
            }
        }

        public static void ValidateProperty(this KeyValuePair<XName, InstanceValue> property)
        {
            property.ValidateProperty(false);
        }

        public static void ValidateProperty(this KeyValuePair<XName, InstanceValue> property, bool allowDelete)
        {
            if (property.Key == null)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.MetadataCannotContainNullKey));
            }
            if (property.Value == null)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.MetadataCannotContainNullValue(property.Key)));
            }
            if (!allowDelete && property.Value.IsDeletedValue)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.InitialMetadataCannotBeDeleted(property.Key)));
            }
        }

        public static bool IsOptional(this InstanceValue value)
        {
            return (value.Options & InstanceValueOptions.Optional) != 0;
        }

        public static bool IsWriteOnly(this InstanceValue value)
        {
            return (value.Options & InstanceValueOptions.WriteOnly) != 0;
        }

        public static ReadOnlyDictionary<XName, InstanceValue> ReadOnlyCopy(this IDictionary<XName, InstanceValue> bag, bool allowWriteOnly)
        {
            if (bag != null && bag.Count > 0)
            {
                Dictionary<XName, InstanceValue> copy = new Dictionary<XName, InstanceValue>(bag.Count);
                foreach (KeyValuePair<XName, InstanceValue> value in bag)
                {
                    value.ValidateProperty();
                    if (!value.Value.IsWriteOnly())
                    {
                        copy.Add(value.Key, value.Value);
                    }
                    else if (!allowWriteOnly)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SRCore.LoadedWriteOnlyValue));
                    }
                }
                return new ReadOnlyDictionary<XName, InstanceValue>(copy);
            }
            else
            {
                return null;
            }
        }

        public static ReadOnlyDictionary<XName, InstanceValue> ReadOnlyMergeInto(this IDictionary<XName, InstanceValue> bag, IDictionary<XName, InstanceValue> existing, bool allowWriteOnly)
        {
            Fx.Assert(existing == null || existing is ReadOnlyDictionary<XName, InstanceValue>, "Should only be merging into other read-only dictionaries.");

            if (bag != null && bag.Count > 0)
            {
                Dictionary<XName, InstanceValue> copy = existing == null ? new Dictionary<XName, InstanceValue>(bag.Count) : new Dictionary<XName, InstanceValue>(existing);
                foreach (KeyValuePair<XName, InstanceValue> value in bag)
                {
                    value.ValidateProperty(true);
                    if (value.Value.IsDeletedValue)
                    {
                        copy.Remove(value.Key);
                    }
                    else if (!value.Value.IsWriteOnly())
                    {
                        copy[value.Key] = value.Value;
                    }
                    else if (!allowWriteOnly)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SRCore.LoadedWriteOnlyValue));
                    }
                    else
                    {
                        copy.Remove(value.Key);
                    }
                }
                return new ReadOnlyDictionary<XName, InstanceValue>(copy);
            }
            else
            {
                return (ReadOnlyDictionary<XName, InstanceValue>)existing;
            }
        }
    }
}
