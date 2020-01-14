// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Test.Common.TestObjects.Utilities.Sql
{
    public class DatabaseConfiguration
    {
        protected List<string> storedProcs;
        protected List<string> tables;
        protected List<string> roles;

        public string CreateSchema
        {
            get;
            set;
        }

        public bool CreateSchemaExists
        {
            get
            {
                if (string.IsNullOrEmpty(this.CreateSchema))
                {
                    ThrowPropNotInitialized("CreateSchema");
                }

                return File.Exists(this.CreateSchema);
            }
        }

        public string CreateLogic
        {
            get;
            set;
        }

        public bool CreateLogicExists
        {
            get
            {
                if (string.IsNullOrEmpty(this.CreateLogic))
                {
                    ThrowPropNotInitialized("CreateLogic");
                }

                return File.Exists(this.CreateLogic);
            }
        }

        public string DropSchema
        {
            get;
            set;
        }

        public bool DropSchemaExists
        {
            get
            {
                if (string.IsNullOrEmpty(this.DropSchema))
                {
                    ThrowPropNotInitialized("DropSchema");
                }

                return File.Exists(this.DropSchema);
            }
        }

        public string UpgradeSchema
        {
            get;
            set;
        }

        public bool UpgradeSchemaExists
        {
            get
            {
                if (string.IsNullOrEmpty(this.UpgradeSchema))
                {
                    ThrowPropNotInitialized("UpgradeSchema");
                }

                return File.Exists(this.UpgradeSchema);
            }
        }

        public string DropLogic
        {
            get;
            set;
        }

        public bool DropLogicExists
        {
            get
            {
                if (string.IsNullOrEmpty(this.DropLogic))
                {
                    ThrowPropNotInitialized("DropLogic");
                }

                return File.Exists(this.DropLogic);
            }
        }

        public List<string> TableNames
        {
            get
            {
                if (this.tables == null)
                {
                    ThrowPropNotInitialized("TableNames");
                }
                return this.tables;
            }
            set
            {
                this.tables = value;
            }
        }

        public List<string> StoredProcedureNames
        {
            get
            {
                if (this.storedProcs == null)
                {
                    ThrowPropNotInitialized("StoredProcedureNames");
                }
                return this.storedProcs;
            }
            set
            {
                this.storedProcs = value;
            }
        }

        public List<string> Roles
        {
            get
            {
                if (this.roles == null)
                {
                    ThrowPropNotInitialized("Roles");
                }
                return this.roles;
            }
            set
            {
                this.roles = value;
            }
        }

        private void ThrowPropNotInitialized(string propName)
        {
            throw new ArgumentNullException(propName, string.Format("{0} was not initialized for {1}", propName, this.GetType()));
        }
    }
}
