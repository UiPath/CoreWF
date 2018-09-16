// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Debugger
{
    using System.Collections.Generic;

    internal partial class CharacterSpottingTextReader : ICharacterSpottingTextReaderForUnitTest
    {
        int ICharacterSpottingTextReaderForUnitTest.CurrentLine
        {
            get { return this.currentLine; }
        }

        int ICharacterSpottingTextReaderForUnitTest.CurrentPosition
        {
            get { return this.currentPosition; }
        }

        List<DocumentLocation> ICharacterSpottingTextReaderForUnitTest.StartBrackets
        {
            get { return this.startAngleBrackets; }
        }

        List<DocumentLocation> ICharacterSpottingTextReaderForUnitTest.EndBrackets
        {
            get { return this.endAngleBrackets; }
        }

        List<DocumentLocation> ICharacterSpottingTextReaderForUnitTest.SingleQuotes
        {
            get { return this.singleQuotes; }
        }

        List<DocumentLocation> ICharacterSpottingTextReaderForUnitTest.DoubleQuotes
        {
            get { return this.doubleQuotes; }
        }
    }
}
