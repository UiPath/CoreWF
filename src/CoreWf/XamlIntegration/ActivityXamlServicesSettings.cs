// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    public class ActivityXamlServicesSettings
    {
        private AheadOfTimeCompiler cSharpCompiler;

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
    }
}
