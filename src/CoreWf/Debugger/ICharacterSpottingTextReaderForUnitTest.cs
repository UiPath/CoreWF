// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Debugger
{
    using System.Collections.Generic;

    internal interface ICharacterSpottingTextReaderForUnitTest
    {
        int CurrentLine { get; }

        int CurrentPosition { get; }

        List<DocumentLocation> StartBrackets { get; }

        List<DocumentLocation> EndBrackets { get; }

        List<DocumentLocation> SingleQuotes { get; }

        List<DocumentLocation> DoubleQuotes { get; }
    }
}
