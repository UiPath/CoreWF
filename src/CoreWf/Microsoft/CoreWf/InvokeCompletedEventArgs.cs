// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CoreWf
{
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
