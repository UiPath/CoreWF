// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration;

public class ActivityXamlServicesSettings
{
    private AheadOfTimeCompiler _cSharpCompiler;
    private AheadOfTimeCompiler _vbCompiler;

    public bool CompileExpressions { get; set; }

    public LocationReferenceEnvironment LocationReferenceEnvironment { get; set; }

    public AheadOfTimeCompiler CSharpCompiler
    {
        get => _cSharpCompiler;
        set
        {
            _cSharpCompiler = value;
            CompileExpressions = value != null;
        }
    }

    public AheadOfTimeCompiler VbCompiler
    {
        get => _vbCompiler;
        set
        {
            _vbCompiler = value;
            CompileExpressions = value != null;
        }
    }

    internal AheadOfTimeCompiler GetCompiler(string language)
    {
        return language switch
        {
            "C#" => CSharpCompiler ?? new CSharpAotCompiler(),
            "VB" => VbCompiler ?? new VbAotCompiler(),
            _    => throw new ArgumentOutOfRangeException(nameof(language), language, "Supported values: C# and VB.")
        };
    }

    /// <summary>
    /// When using compiled expressions, if this is set to true, the compiled expressions functions parameter names are renamed. 
    /// In rare cases, the function parameter name which is "value" by default, conflicts with variables with the same name.
    /// (it could be value, or value1, etc.. ) 
    /// </summary>
    public bool EnableFunctionParameterRename { get; set; } = false;
}
