// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    public class ActivityXamlServicesSettings
    {
        private AheadOfTimeCompiler cSharpCompiler;
        private AheadOfTimeCompiler vbCompiler;

        public bool CompileExpressions
        {
            get;
            set;
        }

        public LocationReferenceEnvironment LocationReferenceEnvironment
        {
            get;
            set;
        }

        public AheadOfTimeCompiler CSharpCompiler
        {
            get => cSharpCompiler;
            set
            {
                cSharpCompiler = value;
                CompileExpressions = value != null;
            }
        }

        public AheadOfTimeCompiler VbCompiler
        {
            get => vbCompiler;
            set
            {
                vbCompiler = value;
                CompileExpressions = value != null;
            }
        }

        internal AheadOfTimeCompiler GetCompiler(string language)
        {
            switch (language)
            {
                case "C#":
                    return CSharpCompiler ?? new CSharpAheadOfTimeCompiler();
                case "VB":
                    return VbCompiler ?? new VbAheadOfTimeCompiler();
            }
            throw new ArgumentOutOfRangeException(nameof(language), language, "Supported values: C# and VB.");
        }
    }
}
