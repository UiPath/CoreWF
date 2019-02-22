// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Runtime;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    [Fx.Tag.XamlVisible(false)]
    public class InvokeCompletedEventArgs : AsyncCompletedEventArgs
    {
        internal InvokeCompletedEventArgs(Exception error, bool cancelled, AsyncInvokeContext context)
            : base(error, cancelled, context.UserState)
        {
            this.Outputs = context.Outputs;
            
        }

        public IDictionary<string, object> Outputs
        {
            get;
            private set;
        }
    }
}
