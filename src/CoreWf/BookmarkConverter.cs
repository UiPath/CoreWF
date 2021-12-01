// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.Activities;

public class BookmarkConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(long);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
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
