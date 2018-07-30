// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Hosting
{
    using CoreWf.Internals;
    using CoreWf.Runtime;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldHaveCorrectSuffix,
    //    Justification = "Approved name")]
    public sealed class SymbolResolver : IDictionary<string, object>
    {
        private Dictionary<string, ExternalLocationReference> symbols;

        public SymbolResolver()
        {
            this.symbols = new Dictionary<string, ExternalLocationReference>();
        }

        public int Count
        {
            get { return this.symbols.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<string> Keys
        {
            get { return this.symbols.Keys; }
        }

        public ICollection<object> Values
        {
            get
            {
                List<object> values = new List<object>(this.symbols.Count);

                foreach (ExternalLocationReference reference in this.symbols.Values)
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
                return this.symbols[key].Value;
            }

            set
            {
                // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
                this.symbols[key] = CreateReference(key, value);
            }
        }

        public void Add(string key, object value)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            this.symbols.Add(key, CreateReference(key, value));
        }

        public void Add(string key, Type type)
        {
            if (type == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(type));
            }

            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            this.symbols.Add(key, new ExternalLocationReference(key, type, TypeHelper.GetDefaultValueForType(type)));
        }

        public void Add(string key, object value, Type type)
        {
            if (type == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(type));
            }

            if (!TypeHelper.AreTypesCompatible(value, type))
            {
                throw FxTrace.Exception.Argument(nameof(value), SR.ValueMustBeAssignableToType);
            }

            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            this.symbols.Add(key, new ExternalLocationReference(key, type, value));
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
            this.symbols.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            if (this.symbols.TryGetValue(item.Key, out ExternalLocationReference reference))
            {
                return item.Value == reference.Value;
            }

            return false;
        }

        public bool ContainsKey(string key)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            return this.symbols.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw FxTrace.Exception.ArgumentOutOfRange(nameof(arrayIndex), arrayIndex, SR.CopyToIndexOutOfRange);
            }

            if (array.Rank > 1)
            {
                throw FxTrace.Exception.Argument(nameof(array), SR.CopyToRankMustBeOne);
            }

            if (this.symbols.Count > array.Length - arrayIndex)
            {
                throw FxTrace.Exception.Argument(nameof(array), SR.CopyToNotEnoughSpaceInArray);
            }

            foreach (KeyValuePair<string, ExternalLocationReference> pair in this.symbols)
            {
                Fx.Assert(arrayIndex < array.Length, "We must have room since we validated it.");

                array[arrayIndex] = new KeyValuePair<string, object>(pair.Key, pair.Value.Value);
                arrayIndex++;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, ExternalLocationReference> pair in this.symbols)
            {
                yield return new KeyValuePair<string, object>(pair.Key, pair.Value.Value);
            }
        }

        internal IEnumerable<KeyValuePair<string, LocationReference>> GetLocationReferenceEnumerator()
        {
            foreach (KeyValuePair<string, ExternalLocationReference> pair in this.symbols)
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
            return this.symbols.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary
            if (this.symbols.TryGetValue(item.Key, out ExternalLocationReference reference))
            {
                if (reference.Value == item.Value)
                {
                    this.symbols.Remove(item.Key);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            // We don't need to check key for null since we want the exception to bubble up from the inner dictionary

            if (this.symbols.TryGetValue(key, out ExternalLocationReference reference))
            {
                value = reference.Value;
                return true;
            }

            value = null;
            return false;
        }

        internal bool TryGetLocationReference(string name, out LocationReference result)
        {
            if (this.symbols.TryGetValue(name, out ExternalLocationReference reference))
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
                if (this.symbols.TryGetValue(locationReference.Name, out ExternalLocationReference externalLocationReference))
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
            if (this.symbols.TryGetValue(name, out ExternalLocationReference reference))
            {
                if (reference.Type == type)
                {
                    // We're the same reference
                    return reference.Location;
                }
            }

            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SymbolResolverDoesNotHaveSymbol(name, type)));
        }

        public LocationReferenceEnvironment AsLocationReferenceEnvironment()
        {
            return new SymbolResolverLocationReferenceEnvironment(this);
        }

        private class SymbolResolverLocationReferenceEnvironment : LocationReferenceEnvironment
        {
            private readonly SymbolResolver symbolResolver;

            public SymbolResolverLocationReferenceEnvironment(SymbolResolver symbolResolver)
            {
                this.symbolResolver = symbolResolver;
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
                    throw FxTrace.Exception.ArgumentNull(nameof(locationReference));
                }

                return this.symbolResolver.IsVisible(locationReference);
            }

            public override bool TryGetLocationReference(string name, out LocationReference result)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
                }

                return this.symbolResolver.TryGetLocationReference(name, out result);
            }

            public override IEnumerable<LocationReference> GetLocationReferences()
            {
                List<LocationReference> list = new List<LocationReference>();
                foreach (ExternalLocationReference item in this.symbolResolver.symbols.Values)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        private class ExternalLocationReference : LocationReference
        {
            private readonly ExternalLocation location;
            private readonly string name;
            private readonly Type type;

            public ExternalLocationReference(string name, Type type, object value)
            {
                this.name = name;
                this.type = type;
                this.location = new ExternalLocation(this.type, value);
            }

            public object Value
            {
                get
                {
                    return this.location.Value;
                }
            }

            public Location Location
            {
                get
                {
                    return this.location;
                }
            }

            protected override string NameCore
            {
                get
                {
                    return this.name;
                }
            }

            protected override Type TypeCore
            {
                get
                {
                    return this.type;
                }
            }

            public override Location GetLocation(ActivityContext context)
            {
                SymbolResolver resolver = context.GetExtension<SymbolResolver>();

                if (resolver == null)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CanNotFindSymbolResolverInWorkflowInstanceExtensions));
                }

                return resolver.GetLocation(this.Name, this.Type);
            }

            private class ExternalLocation : Location
            {
                private readonly Type type;
                private readonly object value;

                public ExternalLocation(Type type, object value)
                {
                    this.type = type;
                    this.value = value;
                }

                public override Type LocationType
                {
                    get
                    {
                        return this.type;
                    }
                }

                protected override object ValueCore
                {
                    get
                    {
                        return this.value;
                    }
                    set
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ExternalLocationsGetOnly));
                    }
                }
            }
        }
    }
}
