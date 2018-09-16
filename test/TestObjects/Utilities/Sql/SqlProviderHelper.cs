// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Test.Common.TestObjects.Utilities.Sql
{
    // SqlProviderHelper does does not use or fallback to using TestParemeters. Callers should already have
    // the required configuration information before creating this class. 
    //
    // The SqlProviderHelper constructor attempts to determine what the tester intended to configure according to the following rules:
    //   1) If connString is null, we use the first sql instance on the localhost
    //   2) Both databaseName and connString parameters cannot be null.
    //   3) If the test specifies both a databaseName and a connString, 
    //          then the 'Initial Catalog' from the connString is used. If 'Initial Catalog' is not specified, we use the databaseName. 
    //   4) If databaseName is null and a connString is specified, 
    //          the database specified by 'Initial Catalog' value is used. If this doesn't exist either, we throw.
    //   5) connString can be partial as long as it contains a 'DataSource' value. SqlInstance fills in the rest of the connString if needed.
    //
    public class SqlProviderHelper : IDisposable
    {
        protected SqlInstance sqlInstance = null;
        protected string databaseName;
        protected DatabaseConfiguration dbConfig;

        public SqlProviderHelper(string databaseName, string connString, DatabaseConfiguration dbConfig)
        {
            if (string.IsNullOrEmpty(databaseName) && string.IsNullOrEmpty(connString))
            {
                throw new ArgumentNullException("databaseName", "'databaseName' cannot be null if 'connString' is null");
            }

            this.dbConfig = dbConfig;

            if (!string.IsNullOrEmpty(connString))
            {
                this.sqlInstance = new SqlInstance(connString);

                // if the 'Initial Catalog value' is not specified we try to use the databaseName. If databaseName is null, then we throw.
                if (string.IsNullOrEmpty(this.sqlInstance.DatabaseName))
                {
                    if (string.IsNullOrEmpty(databaseName))
                    {
                        throw new ArgumentNullException("connString", "must specify a 'Initial Catalog' value or use the databaseName parameter");
                    }

                    this.databaseName = databaseName;
                }
                else
                {
                    this.databaseName = this.sqlInstance.DatabaseName;
                }
            }
            else // They didn't specify a connString, so grab the first instance on the localhost
            {
                this.databaseName = databaseName;

                List<SqlInstance> sqlInstances = SqlInstanceFactory.RetrieveInstances("localhost", null, true);
                if (sqlInstances != null && sqlInstances.Count > 0)
                {
                    this.sqlInstance = sqlInstances[0];
                }
                else
                {
                    throw new Exception("Unable to retrieve Sql Instance, check to ensure Sql is installed");
                }
            }

            this.sqlInstance.SetDatabase(this.databaseName);
        }

        public virtual string SqlInstanceInfo
        {
            get
            {
                if (this.sqlInstance != null)
                {
                    return string.Format("[SqlProviderHelper] Database connection: \"{0}\"", this.ConnectionString);
                }
                return string.Empty;
            }
        }

        public virtual string ConnectionString
        {
            get
            {
                if (this.sqlInstance != null)
                {
                    return this.sqlInstance.GenerateConnectionString();
                }
                return string.Empty;
            }
        }

        public virtual bool DatabaseExists()
        {
            bool bRet = this.sqlInstance.CheckExistDatabase(this.databaseName);
            if (bRet)
            {
                this.sqlInstance.SetDatabase(this.databaseName);
            }
            else
            {
                //Log.TraceInternal("[SqlProviderHelper] Database not found " + this.databaseName);
            }

            return bRet;
        }

        public virtual bool TablesExist()
        {
            // returns true if all tables exist and the expected count matches
            return this.sqlInstance.CheckObjectsExistInDatabase(SqlObjectType.Table, this.dbConfig.TableNames, true);
        }

        public virtual bool StoredProceduresExist()
        {
            // returns true if all sprocs exist 
            // There are certain Sql generated sprocs that we can't know the name of ahead of time, so we don't check count mismatch here
            return this.sqlInstance.CheckObjectsExistInDatabase(SqlObjectType.StoredProcedure, this.dbConfig.StoredProcedureNames, false);
        }

        public virtual void CreateDatabase()
        {
            this.sqlInstance.CreateDatabase(this.databaseName);
        }

        public virtual void CreateSchema()
        {
            this.sqlInstance.ExecuteSqlFile(this.dbConfig.CreateSchema);
        }

        public virtual void CreateLogic()
        {
            this.sqlInstance.ExecuteSqlFile(this.dbConfig.CreateLogic);
        }

        public virtual void CleanupTables()
        {
            foreach (string tableName in this.dbConfig.TableNames)
            {
                this.sqlInstance.ExecuteSqlString("[SqlProviderHelper] Truncate table " + tableName);
            }
        }

        public virtual void DropDatabase()
        {
            this.sqlInstance.DropDatabase();
        }

        public virtual void UpgradeDatabase()
        {
            if (this.dbConfig.UpgradeSchemaExists)
            {
                this.sqlInstance.ExecuteSqlFile(this.dbConfig.UpgradeSchema);
            }
            else
            {
                // Log.WarnInternal("[SqlProviderHelper] File not found at " + PartialTrustPath.GetFullPath(this.dbConfig.UpgradeSchema));
            }
        }

        #region IDisposable Members

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing)
                {
                    if (sqlInstance != null)
                    {
                        this.sqlInstance.Dispose();
                        sqlInstance = null;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
