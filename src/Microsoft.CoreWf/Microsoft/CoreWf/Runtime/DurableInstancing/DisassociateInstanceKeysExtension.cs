// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    internal class DisassociateInstanceKeysExtension
    {
        private bool _automaticDisassociationEnabled;

        public DisassociateInstanceKeysExtension()
        {
            _automaticDisassociationEnabled = false;
        }

        public bool AutomaticDisassociationEnabled
        {
            get
            {
                return _automaticDisassociationEnabled;
            }

            set
            {
                _automaticDisassociationEnabled = value;
            }
        }
    }
}
