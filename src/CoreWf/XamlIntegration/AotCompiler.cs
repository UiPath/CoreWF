// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System.CodeDom;
    using System.CodeDom.Compiler;

    public abstract class AotCompiler
    {
        public abstract CompilerResults Compile(CompilerParameters options, CodeCompileUnit compilationUnit);
    }
}