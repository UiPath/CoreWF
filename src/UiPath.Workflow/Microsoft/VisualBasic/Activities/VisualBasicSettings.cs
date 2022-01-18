// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.VisualBasic.Activities.XamlIntegration;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Activities.Runtime;
using System.Windows.Markup;
using System.Xaml;
using System.ComponentModel;
using System.Activities.Internals;
using System.Reflection;

namespace Microsoft.VisualBasic.Activities
{
    using CompilerFactory = Func<HashSet<Assembly>, JustInTimeCompiler>;

    [ValueSerializer(typeof(VisualBasicSettingsValueSerializer))]
    [TypeConverter(typeof(VisualBasicSettingsConverter))]
    public class VisualBasicSettings
    {

        static readonly HashSet<VisualBasicImportReference> defaultImportReferences = new HashSet<VisualBasicImportReference>()
        {
            //"mscorlib"
            new VisualBasicImportReference { Import = "System", Assembly = typeof(object).Assembly.FullName },
            new VisualBasicImportReference { Import = "System.Collections", Assembly = "System.Runtime" },
            new VisualBasicImportReference { Import = "System.Collections.Generic", Assembly = "System.Runtime" },
            //"system"
            new VisualBasicImportReference { Import = "System.ComponentModel", Assembly = typeof(BrowsableAttribute).Assembly.FullName },
            new VisualBasicImportReference { Import = "System.Linq.Expressions", Assembly = typeof(System.Linq.Expressions.Expression).Assembly.FullName },
            new VisualBasicImportReference { Import = "System", Assembly = "system" },
            new VisualBasicImportReference { Import = "System.Collections.Generic", Assembly = typeof(HashSet<>).Assembly.FullName },
            //"System.Activities"
            new VisualBasicImportReference { Import = "System.Activities", Assembly = "System.Activities" },
            new VisualBasicImportReference { Import = "System.Activities.Statements", Assembly = "System.Activities" },
            new VisualBasicImportReference { Import = "System.Activities.Expressions", Assembly = "System.Activities" },
            // Microsoft.VisualBasic
            new VisualBasicImportReference { Import = "Microsoft.VisualBasic", Assembly = typeof(CompilerServices.Conversions).Assembly.FullName },
        };

        static VisualBasicSettings defaultSettings = new VisualBasicSettings(defaultImportReferences);
        private CompilerFactory _compilerFactory = references => new VbJitCompiler(references);

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

        public CompilerFactory CompilerFactory
        {
            get => _compilerFactory;
            set
            {
                _compilerFactory = value;
                CompilerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        internal bool SuppressXamlSerialization
        {
            get;
            set;
        }

        internal event EventHandler CompilerChanged;

        internal static JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => Default.CompilerFactory(references);

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
