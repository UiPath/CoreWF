// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Statements;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class CompensationToken
{
    internal const string PropertyName = "System.Compensation.CompensationToken";
    internal const long RootCompensationId = 0;
            
    internal CompensationToken(CompensationTokenData tokenData)
    {
        CompensationId = tokenData.CompensationId;
    }
        
    [DataMember(EmitDefaultValue = false)]
    internal long CompensationId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal bool CompensateCalled { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal bool ConfirmCalled { get; set; }
}
