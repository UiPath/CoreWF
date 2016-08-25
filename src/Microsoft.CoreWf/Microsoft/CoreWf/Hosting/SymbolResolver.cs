// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CoreWf.Hosting
{
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldHaveCorrectSuffix,
    //Justification = "Approved name")]
    public sealed class SymbolResolver : IDictionary<string, object>
    {
        private Dictionary<string, ExternalLocationReference> _symbols;

        public SymbolResolver()
        {
            _symbols = new Dictionary<string, ExternalLocationReference>();
        }

        public int Count
        {
            get { return _symbols.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<string> Keys
        {
            get { return _symbols.Keys; }
        }

        public ICollection<object> Values
        {
            get
            {
                List<object> values = new List<object>(_symbols.Count);

                foreach (ExternalLocationReference reference in _symbols.Values)
                {
                    values.Add(reference.Value);
                }

                return values;
            }
        }

        public object this[string key]
        {
            get
            {
                // We don't need to do any existence checks since we want the dictionary exception to bubble up
                return _symbols[key].Value;
            }

            set
            {
                // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
                _symbols[key] = CreateReference(key, value);
            }
        }

        public void Add(string key, object value)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            _symbols.Add(key, CreateReference(key, value));
        }

        public void Add(string key, Type type)
        {
            if (type == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("type");
            }

            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            _symbols.Add(key, new ExternalLocationReference(key, type, TypeHelper.GetDefaultValueForType(type)));
        }

        public void Add(string key, object value, Type type)
        {
            if (type == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("type");
            }

            if (!TypeHelper.AreTypesCompatible(value, type))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("value", SR.ValueMustBeAssignableToType);
            }

            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            _symbols.Add(key, new ExternalLocationReference(key, type, value));
        }

        private ExternalLocationReference CreateReference(string name, object value)
        {
            Type valueType = TypeHelper.ObjectType;

            if (value != null)
            {
                valueType = value.GetType();
            }

            return new ExternalLocationReference(name, valueType, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _symbols.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            ExternalLocationReference reference;
            if (_symbols.TryGetValue(item.Key, out reference))
            {
                return item.Value == reference.Value;
            }

            return false;
        }

        public bool ContainsKey(string key)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            return _symbols.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("array");
            }

            if (arrayIndex < 0)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentOutOfRange("arrayIndex", arrayIndex, SR.CopyToIndexOutOfRange);
            }

            if (array.Rank > 1)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("array", SR.CopyToRankMustBeOne);
            }

            if (_symbols.Count > array.Length - arrayIndex)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("array", SR.CopyToNotEnoughSpaceInArray);
            }

            foreach (KeyValuePair<string, ExternalLocationReference> pair in _symbols)
            {
                Fx.Assert(arrayIndex < array.Length, "We must have room since we validated it.");

                array[arrayIndex] = new KeyValuePair<string, object>(pair.Key, pair.Value.Value);
                arrayIndex++;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, ExternalLocationReference> pair in _symbols)
            {
                yield return new KeyValuePair<string, object>(pair.Key, pair.Value.Value);
            }
        }

        internal IEnumerable<KeyValuePair<string, LocationReference>> GetLocationReferenceEnumerator()
        {
            foreach (KeyValuePair<string, ExternalLocationReference> pair in _symbols)
            {
                yield return new KeyValuePair<string, LocationReference>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(string key)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            return _symbols.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            ExternalLocationReference reference;
            if (_symbols.TryGetValue(item.Key, out reference))
            {
                if (reference.Value == item.Value)
                {
                    _symbols.Remove(item.Key);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary

            ExternalLocationReference reference;
            if (_symbols.TryGetValue(key, out reference))
            {
                value = reference.Value;
                return true;
            }

            value = null;
            return false;
        }

        internal bool TryGetLocationReference(string name, out LocationReference result)
        {
            ExternalLocationReference reference;
            if (_symbols.TryGetValue(name, out reference))
            {
                result = reference;
                return true;
            }

            result = null;
            return false;
        }

        internal bool IsVisible(LocationReference locationReference)
        {
            // We only check for null since string.Empty is
            // actually allowed.
            if (locationReference.Name == null)
            {
                return false;
            }
            else
            {
                ExternalLocationReference externalLocationReference;
                if (_symbols.TryGetValue(locationReference.Name, out externalLocationReference))
                {
                    if (externalLocationReference.Type == locationReference.Type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Location GetLocation(string name, Type type)
        {
            ExternalLocationReference reference;
            if (_symbols.TryGetValue(name, out reference))
            {
                if (reference.Type == type)
                {
                    // We're the same reference
                    return reference.Location;
                }
            }

            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.SymbolResolverDoesNotHaveSymbol(name, type)));
        }

        public LocationReferenceEnvironment AsLocationReferenceEnvironment()
        {
            return new SymbolResolverLocationReferenceEnvironment(this);
        }

        private class SymbolResolverLocationReferenceEnvironment : LocationReferenceEnvironment
        {
            private SymbolResolver _symbolResolver;

            public SymbolResolverLocationReferenceEnvironment(SymbolResolver symbolResolver)
            {
                _symbolResolver = symbolResolver;
            }

            public override Activity Root
            {
                get
                {
                    return null;
                }
            }

            public override bool IsVisible(LocationReference locationReference)
            {
                if (locationReference == null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationReference");
                }

                return _symbolResolver.IsVisible(locationReference);
            }

            public override bool TryGetLocationReference(string name, out LocationReference result)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
                }

                return _symbolResolver.TryGetLocationReference(name, out result);
            }

            public override IEnumerable<LocationReference> GetLocationReferences()
            {
                List<LocationReference> list = new List<LocationReference>();
                foreach (ExternalLocationReference item in _symbolResolver._symbols.Values)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        private class ExternalLocationReference : LocationReference
        {
            private ExternalLocation _location;
            private string _name;
            private Type _type;

            public ExternalLocationReference(string name, Type type, object value)
            {
                _name = name;
                _type = type;
                _location = new ExternalLocation(_type, value);
            }

            public object Value
            {
                get
                {
                    return _location.Value;
                }
            }

            public Location Location
            {
                get
                {
                    return _location;
                }
            }

            protected override string NameCore
            {
                get
                {
                    return _name;
                }
            }

            protected override Type TypeCore
            {
                get
                {
                    return _type;
                }
            }

            public override Location GetLocation(ActivityContext context)
            {
                SymbolResolver resolver = context.GetExtension<SymbolResolver>();

                if (resolver == null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CanNotFindSymbolResolverInWorkflowInstanceExtensions));
                }

                return resolver.GetLocation(this.Name, this.Type);
            }

            private class ExternalLocation : Location
            {
                private Type _type;
                private object _value;

                public ExternalLocation(Type type, object value)
                {
                    _type = type;
                    _value = value;
                }

                public override Type LocationType
                {
                    get
                    {
                        return _type;
                    }
                }

                protected override object ValueCore
                {
                    get
                    {
                        return _value;
                    }
                    set
                    {
                        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ExternalLocationsGetOnly));
                    }
                }
            }
        }
    }
}
