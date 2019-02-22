// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class TextExpressionCompilerResults
    {
        private ReadOnlyCollection<TextExpressionCompilerError> messages;

        internal TextExpressionCompilerResults()
        {
        }

        public Type ResultType
        {
            get;
            internal set;
        }

        public bool HasErrors
        {
            get;
            internal set;
        }

        public bool HasSourceInfo
        {
            get;
            internal set;
        }

        public ReadOnlyCollection<TextExpressionCompilerError> CompilerMessages
        {
            get
            {
                return this.messages;
            }
        }

        internal void SetMessages(IList<TextExpressionCompilerError> messages, bool hasErrors)
        {
            this.messages = new ReadOnlyCollection<TextExpressionCompilerError>(messages);
            this.HasErrors = hasErrors;
        }
    }
}
