// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Resources;
using System.Runtime.CompilerServices;

namespace System.Activities
{
    internal abstract class StringResourceBase
    {
        protected internal abstract ResourceManager ResourceManager { get; }

        public string this[string name] => GetResourceString(name, null);

        public string this[string name, object p1] => Format(GetResourceString(name, null), p1);

        public string this[string name, object p1, object p2] => Format(GetResourceString(name, null), p1, p2);

        public string this[string name, object p1, object p2, object p3] => Format(GetResourceString(name, null), p1, p2, p3);

        public string this[string name, params object[] arguments] => Format(GetResourceString(name, null), arguments);

        // This method is used to decide if we need to append the parameters to the message when calling SR.Format. 
        // by default it returns false.
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal bool UsingResourceKeys()
        {
            return false;
        }

        internal string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey); }
            catch (MissingManifestResourceException) { }

            if (defaultString != null && resourceKey.Equals(resourceString, StringComparison.Ordinal))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + String.Join(", ", args);
                }

                return String.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1);
            }

            return String.Format(resourceFormat, p1);
        }

        internal string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1, p2);
            }

            return String.Format(resourceFormat, p1, p2);
        }

        internal string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1, p2, p3);
            }
            return String.Format(resourceFormat, p1, p2, p3);
        }
    }
}