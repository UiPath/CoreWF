// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Runtime.DurableInstancing
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
