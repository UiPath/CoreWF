// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Globalization;
    using System.ComponentModel;

    public class BookmarkConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(long); 
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {

            if (value is string stringValue && !String.IsNullOrEmpty(stringValue))
            {
                return new Bookmark(stringValue);
            }

            if (value is long longValue && longValue != 0)
            {
                return Bookmark.Create(longValue);
            }

            return base.ConvertFrom(context, culture, value);
        }
        
    }
}
