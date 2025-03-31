// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration;

public class TextExpressionCompilerSettings
{
    public TextExpressionCompilerSettings()
    {
        GenerateAsPartialClass = true;
        AlwaysGenerateSource = true;
        ForImplementation = true;
    }

    public AheadOfTimeCompiler Compiler { get; set; }

    public Activity Activity { get; set; }

    public string ActivityName { get; set; }

    public string ActivityNamespace { get; set; }

    public bool AlwaysGenerateSource { get; set; }

    public bool ForImplementation { get; set; }

    public string Language { get; set; }

    public string RootNamespace { get; set; }

    public Action<string> LogSourceGenerationMessage { get; set; }

    public bool GenerateAsPartialClass { get; set; }

    /// <summary>
    /// When set to true, the compiled expressions functions parameter names are renamed. 
    /// In rare cases, the function parameter name which is "value" by default, conflicts with variables with the same name.
    /// (it could be value, or value1, etc.. ) 
    /// </summary>
    public bool EnableFunctionParameterRename { get; set; } = false;
}
