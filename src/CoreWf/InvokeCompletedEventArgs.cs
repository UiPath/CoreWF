// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public class InvokeCompletedEventArgs : AsyncCompletedEventArgs
{
    internal InvokeCompletedEventArgs(Exception error, bool cancelled, AsyncInvokeContext context)
        : base(error, cancelled, context.UserState)
    {
        Outputs = context.Outputs;            
    }

    public IDictionary<string, object> Outputs { get; private set; }
}
