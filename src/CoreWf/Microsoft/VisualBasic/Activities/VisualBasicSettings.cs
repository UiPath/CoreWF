//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.VisualBasic.Activities
{
    using Microsoft.VisualBasic.Activities.XamlIntegration;
    using System;
    using CoreWf;
    using System.Collections.Generic;
    using CoreWf.Runtime;
    using Portable.Xaml.Markup;
    using Portable.Xaml;
    using System.ComponentModel;
    using System.Reflection;
    using CoreWf.Internals;

    [ValueSerializer(typeof(VisualBasicSettingsValueSerializer))]
    [TypeConverter(typeof(VisualBasicSettingsConverter))]
    public class VisualBasicSettings
    {
        
        static readonly HashSet<VisualBasicImportReference> defaultImportReferences = new HashSet<VisualBasicImportReference>()
        {
            //"mscorlib"
            new VisualBasicImportReference { Import = "System", Assembly = "mscorlib" },
            new VisualBasicImportReference { Import = "System.Collections", Assembly = "mscorlib" },
            new VisualBasicImportReference { Import = "System.Collections.Generic", Assembly = "mscorlib" },
            //"system"
            new VisualBasicImportReference { Import = "System", Assembly = "system" },
            new VisualBasicImportReference { Import = "System.Collections.Generic", Assembly = "system" },
            //"System.Activities"
            new VisualBasicImportReference { Import = "CoreWf", Assembly = "CoreWf" },
            new VisualBasicImportReference { Import = "CoreWf.Statements", Assembly = "CoreWf" },
            new VisualBasicImportReference { Import = "CoreWf.Expressions", Assembly = "CoreWf" },
        };

        static VisualBasicSettings defaultSettings = new VisualBasicSettings(defaultImportReferences);

        public VisualBasicSettings()
        {
            this.ImportReferences = new HashSet<VisualBasicImportReference>();
        }

        VisualBasicSettings(HashSet<VisualBasicImportReference> importReferences)
        {
            Fx.Assert(importReferences != null, "caller must verify");
            this.ImportReferences = new HashSet<VisualBasicImportReference>(importReferences);
        }

        public static VisualBasicSettings Default
        {
            get
            {
                return defaultSettings;
            }
        }

        // hide from XAML since the value serializer can't suppress yet
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<VisualBasicImportReference> ImportReferences
        {
            get;
            private set;
        }

        internal bool SuppressXamlSerialization 
        { 
            get; 
            set; 
        }
        
        internal void GenerateXamlReferences(IValueSerializerContext context)
        {
            // promote settings to xmlns declarations
            INamespacePrefixLookup namespaceLookup = GetService<INamespacePrefixLookup>(context);
            foreach (VisualBasicImportReference importReference in this.ImportReferences)
            {
                importReference.GenerateXamlNamespace(namespaceLookup);
            }
        }

        internal static T GetService<T>(ITypeDescriptorContext context) where T : class
        {
            T service = (T)context.GetService(typeof(T));
            if (service == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidTypeConverterUsage));
            }

            return service;
        }
    }
}
