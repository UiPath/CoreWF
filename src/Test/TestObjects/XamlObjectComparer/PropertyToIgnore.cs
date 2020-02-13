// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TestObjects.XamlObjectComparer
{
    public class PropertyToIgnore
    {

        private IgnoreProperty _whatToIgnore;
        private string _owner;
        public PropertyToIgnore()
        {
            this._owner = null;
        }

        public string Owner
        {
            set
            {
                this._owner = value;
            }
            get
            {
                return this._owner;
            }
        }

        public IgnoreProperty WhatToIgnore
        {
            set
            {
                this._whatToIgnore = value;
            }
            get
            {
                return this._whatToIgnore;
            }
        }
    }
}
