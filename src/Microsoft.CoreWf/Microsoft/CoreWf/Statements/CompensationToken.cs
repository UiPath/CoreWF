// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Statements
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class CompensationToken
    {
        internal const string PropertyName = "System.Compensation.CompensationToken";
        internal const long RootCompensationId = 0;

        internal CompensationToken(CompensationTokenData tokenData)
        {
            this.CompensationId = tokenData.CompensationId;
        }

        [DataMember(EmitDefaultValue = false)]
        internal long CompensationId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        internal bool CompensateCalled
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        internal bool ConfirmCalled
        {
            get;
            set;
        }
    }
}
