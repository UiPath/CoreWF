// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    public class ActivityXamlServicesSettings
    {
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

        public AheadOfTimeCompiler VbCompiler { get; set; }
        public AheadOfTimeCompiler CSharpCompiler { get; set; }

        internal AheadOfTimeCompiler GetCompiler(string language)
        {
            switch (language)
            {
                case "VB":
                    return VbCompiler;
                case "C#":
                    return CSharpCompiler;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language. Supported values : VB and C#."); 
            }
        }
    }
}
