// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Hosting;
using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public class Bookmark : IEquatable<Bookmark>
    {
        private static Bookmark s_asyncOperationCompletionBookmark = new Bookmark(-1);
        private static IEqualityComparer<Bookmark> s_comparer;

        //Used only when exclusive scopes are involved
        private ExclusiveHandleList _exclusiveHandlesThatReferenceThis;

        private long _id;

        private string _externalName;

        private Bookmark(long id)
        {
            Fx.Assert(id != 0, "id should not be zero");
            _id = id;
        }

        internal Bookmark() { }

        public Bookmark(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
            }

            _externalName = name;
        }

        internal static Bookmark AsyncOperationCompletionBookmark
        {
            get
            {
                return s_asyncOperationCompletionBookmark;
            }
        }

        internal static IEqualityComparer<Bookmark> Comparer
        {
            get
            {
                if (s_comparer == null)
                {
                    s_comparer = new BookmarkComparer();
                }

                return s_comparer;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "exclusiveHandlesThatReferenceThis", Order = 2)]
        internal ExclusiveHandleList SerializedExclusiveHandlesThatReferenceThis
        {
            get { return _exclusiveHandlesThatReferenceThis; }
            set { _exclusiveHandlesThatReferenceThis = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "id", Order = 0)]
        internal long SerializedId
        {
            get { return _id; }
            set { _id = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "externalName", Order = 1)]
        internal string SerializedExternalName
        {
            get { return _externalName; }
            set { _externalName = value; }
        }

        [DataMember(EmitDefaultValue = false)]
        internal BookmarkScope Scope
        {
            get;
            set;
        }

        internal bool IsNamed
        {
            get
            {
                return _id == 0;
            }
        }

        public string Name
        {
            get
            {
                if (this.IsNamed)
                {
                    return _externalName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        internal long Id
        {
            get
            {
                Fx.Assert(!this.IsNamed, "We should only get the id for unnamed bookmarks.");

                return _id;
            }
        }

        internal ExclusiveHandleList ExclusiveHandles
        {
            get
            {
                return _exclusiveHandlesThatReferenceThis;
            }
            set
            {
                _exclusiveHandlesThatReferenceThis = value;
            }
        }


        internal static Bookmark Create(long id)
        {
            return new Bookmark(id);
        }

        internal BookmarkInfo GenerateBookmarkInfo(BookmarkCallbackWrapper bookmarkCallback)
        {
            Fx.Assert(this.IsNamed, "Can only generate BookmarkInfo for external bookmarks");

            BookmarkScopeInfo scopeInfo = null;

            if (this.Scope != null)
            {
                scopeInfo = this.Scope.GenerateScopeInfo();
            }

            return new BookmarkInfo(_externalName, bookmarkCallback.ActivityInstance.Activity.DisplayName, scopeInfo);
        }

        public bool Equals(Bookmark other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (this.IsNamed)
            {
                return other.IsNamed && _externalName == other._externalName;
            }
            else
            {
                return _id == other._id;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Bookmark);
        }

        public override int GetHashCode()
        {
            if (this.IsNamed)
            {
                return _externalName.GetHashCode();
            }
            else
            {
                return _id.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (this.IsNamed)
            {
                return this.Name;
            }
            else
            {
                return this.Id.ToString(CultureInfo.InvariantCulture);
            }
        }

        [DataContract]
        internal class BookmarkComparer : IEqualityComparer<Bookmark>
        {
            public BookmarkComparer()
            {
            }

            public bool Equals(Bookmark x, Bookmark y)
            {
                if (object.ReferenceEquals(x, null))
                {
                    return object.ReferenceEquals(y, null);
                }

                return x.Equals(y);
            }

            public int GetHashCode(Bookmark obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
