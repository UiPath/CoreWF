// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Hosting;
using CoreWf.Runtime;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class BookmarkScope : IEquatable<BookmarkScope>
    {
        private static BookmarkScope s_defaultBookmarkScope;

        private long _temporaryId;

        private Guid _id;

        private int _handleReferenceCount;

        internal BookmarkScope(long temporaryId)
        {
            Fx.Assert(temporaryId != default(long), "Should never call this constructor with the default value.");
            _temporaryId = temporaryId;
        }

        public BookmarkScope(Guid id)
        {
            _id = id;
        }

        private BookmarkScope()
        {
            // Only called for making the default sub instance
            // which has an Id of Guid.Empty
        }

        public bool IsInitialized
        {
            get
            {
                return _temporaryId == default(long);
            }
        }

        public Guid Id
        {
            get
            {
                return _id;
            }
            internal set
            {
                Fx.Assert(value != Guid.Empty, "Cannot set this to Guid.Empty.");
                Fx.Assert(!this.IsInitialized, "Can only set this when uninitialized.");

                _id = value;
                _temporaryId = default(long);
            }
        }

        internal int IncrementHandleReferenceCount()
        {
            return ++_handleReferenceCount;
        }

        internal int DecrementHandleReferenceCount()
        {
            return --_handleReferenceCount;
        }

        [DataMember(EmitDefaultValue = false, Name = "temporaryId")]
        internal long SerializedTemporaryId
        {
            get { return _temporaryId; }
            set { _temporaryId = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "id")]
        internal Guid SerializedId
        {
            get { return _id; }
            set { _id = value; }
        }

        internal long TemporaryId
        {
            get
            {
                return _temporaryId;
            }
        }

        public static BookmarkScope Default
        {
            get
            {
                if (s_defaultBookmarkScope == null)
                {
                    s_defaultBookmarkScope = new BookmarkScope();
                }

                return s_defaultBookmarkScope;
            }
        }

        internal bool IsDefault
        {
            get
            {
                // In the strictest sense the default is not initiailized.
                // The Default BookmarkScope is really just a loose reference
                // to the instance specific default that you can get to
                // through NativeActivityContext.DefaultBookmarkScope.
                // We use a scope initialized to Guid.Empty to signify this
                // "loose reference".
                return this.IsInitialized && _id == Guid.Empty;
            }
        }

        public void Initialize(NativeActivityContext context, Guid id)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            if (id == Guid.Empty)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("id");
            }

            if (this.IsInitialized)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeAlreadyInitialized));
            }

            context.InitializeBookmarkScope(this, id);
        }

        public override int GetHashCode()
        {
            if (this.IsInitialized)
            {
                return _id.GetHashCode();
            }
            else
            {
                return _temporaryId.GetHashCode();
            }
        }

        internal BookmarkScopeInfo GenerateScopeInfo()
        {
            if (this.IsInitialized)
            {
                return new BookmarkScopeInfo(this.Id);
            }
            else
            {
                return new BookmarkScopeInfo(_temporaryId.ToString(CultureInfo.InvariantCulture));
            }
        }

        public bool Equals(BookmarkScope other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.IsDefault)
            {
                return other.IsDefault;
            }
            else if (this.IsInitialized)
            {
                Fx.Assert(_id != Guid.Empty, "If we're not the default but we're initialized then we must have a non-Empty Guid.");

                if (other._id == _id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Fx.Assert(_temporaryId != 0, "We should have a non-zero temp id if we're not the default and not initialized.");

                if (other._temporaryId == _temporaryId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
