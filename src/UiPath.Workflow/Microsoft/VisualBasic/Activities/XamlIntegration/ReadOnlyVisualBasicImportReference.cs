namespace Microsoft.VisualBasic.Activities.XamlIntegration
{
    using global::Microsoft.VisualBasic.Activities;
    using System.Activities.Expressions;
    using System.Activities.Runtime;
    using System.Reflection;
    using System.Security;

    [Fx.Tag.SecurityNote(Critical = "Critical because we are accessing a VisualBasicImportReference that is stored in the XmlnsMappings cache, which is SecurityCritical.",
                Safe = "Safe because we are wrapping the VisualBasicImportReference and not allowing unsafe code to modify it.")]
    [SecuritySafeCritical]
    struct ReadOnlyVisualBasicImportReference
    {
        readonly VisualBasicImportReference wrappedReference;

        internal ReadOnlyVisualBasicImportReference(VisualBasicImportReference referenceToWrap)
        {
            this.wrappedReference = referenceToWrap;
        }

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //internal string Assembly
        //{
        //    get
        //    {
        //        return this.wrappedReference.Assembly;
        //    }
        //}

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //internal string Import
        //{
        //    get
        //    {
        //        return this.wrappedReference.Import;
        //    }
        //}

        internal Assembly EarlyBoundAssembly
        {
            get { return this.wrappedReference.EarlyBoundAssembly; }
        }

        internal VisualBasicImportReference Clone()
        {
            return this.wrappedReference.Clone();
        }

        // this code is borrowed from XamlSchemaContext
        internal bool AssemblySatisfiesReference(AssemblyName assemblyName)
        {
            if (this.wrappedReference.AssemblyName.Name != assemblyName.Name)
            {
                return false;
            }
            if (this.wrappedReference.AssemblyName.Version != null && !this.wrappedReference.AssemblyName.Version.Equals(assemblyName.Version))
            {
                return false;
            }
            if (this.wrappedReference.AssemblyName.CultureInfo != null && !this.wrappedReference.AssemblyName.CultureInfo.Equals(assemblyName.CultureInfo))
            {
                return false;
            }
            byte[] requiredToken = this.wrappedReference.AssemblyName.GetPublicKeyToken();
            if (requiredToken != null)
            {
                byte[] actualToken = assemblyName.GetPublicKeyToken();
                if (!AssemblyNameEqualityComparer.IsSameKeyToken(requiredToken, actualToken))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return this.wrappedReference.GetHashCode();
        }

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //public bool Equals(VisualBasicImportReference other)
        //{
        //    return this.wrappedReference.Equals(other);
        //}
    }
}
