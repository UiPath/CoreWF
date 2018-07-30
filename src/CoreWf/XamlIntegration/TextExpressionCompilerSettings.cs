// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;

    public class TextExpressionCompilerSettings
    {
        public TextExpressionCompilerSettings()
        {
            this.GenerateAsPartialClass = true;
            this.AlwaysGenerateSource = true;
            this.ForImplementation = true;
        }

        public Activity Activity
        {
            get;
            set;
        }
        
        public string ActivityName
        {
            get;
            set;
        }
        
        public string ActivityNamespace
        {
            get;
            set;
        }

        public bool AlwaysGenerateSource
        {
            get;
            set;
        }

        public bool ForImplementation
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }
        
        public string RootNamespace
        {
            get;
            set;
        }
                        
        public Action<string> LogSourceGenerationMessage
        {
            get;
            set;
        }

        public bool GenerateAsPartialClass
        {
            get;
            set;
        }
    }
}
