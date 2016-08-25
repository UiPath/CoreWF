// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace Test.Common.TestObjects.Utilities.Sql
{
    public enum SqlObjectType
    {
        Table,
        View,
        StoredProcedure
    }

    public class SqlInstance : IDisposable
    {
        private string _databaseName;
        private string _sqlInstanceName;
        private string _serverHostName;

        private string _connString;

        private SqlConnection _connection;
        private SqlConnection _masterConnection;
        private bool _disposed;

        #region ctors

        // default constructor gives you the local default sql instance
        public SqlInstance()
            : this("LOCALHOST", string.Empty)
        {
        }

        // default database is "master"
        public SqlInstance(string serverHostName, string sqlInstanceName)
            : this(serverHostName, sqlInstanceName, "master")
        {
        }

        // IntegratedSecurity and AsyncProcessing are always set to true except in azure runs
        public SqlInstance(string serverHostName, string sqlInstanceName, string databaseName)
        {
            // if sqlInstanceName is null, we use the default instance
            if (sqlInstanceName == null)
            {
                sqlInstanceName = string.Empty;
            }

            // If serverName is null, we use localhost
            if (serverHostName == null)
            {
                serverHostName = "LOCALHOST";
            }

            if (databaseName == null)
            {
                databaseName = "master";
            }

            if ((serverHostName.ToUpperInvariant().CompareTo("LOCALHOST") == 0) || (serverHostName.CompareTo(".") == 0))
            {
                // set to local machine
                //this.serverHostName = PartialTrustDNS.GetHostName().ToUpperInvariant();
            }
            else
            {
                _serverHostName = serverHostName.ToUpperInvariant();
            }

            _sqlInstanceName = sqlInstanceName;
            _databaseName = databaseName;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = string.IsNullOrEmpty(_sqlInstanceName) ? _serverHostName : _serverHostName + @"\" + _sqlInstanceName;
            builder.InitialCatalog = string.IsNullOrEmpty(this.DatabaseName) ? "master" : _databaseName;
            //builder.IntegratedSecurity = (TestParameters.IsSqlAzureRun) ? false : true;
            //builder.AsynchronousProcessing = builder.InitialCatalog == "master" ? false : true;

            _connString = builder.ConnectionString;
        }

        // connString must at a minimum specify a 'Data Source' value
        // IntegratedSecurity and AsyncProcessing are always initialized to true except in azure runs
        public SqlInstance(string connString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connString);
            //builder.IntegratedSecurity = (TestParameters.IsSqlAzureRun) ? false : true;
            //builder.AsynchronousProcessing = true;

            if (string.IsNullOrEmpty(builder.DataSource))
            {
                throw new ArgumentException("'Data Source' must be specified in connection string", "connString");
            }

            // DataSource can either be a "serverName" (i.e. clarkr-testmachine) or "serverName\sqlInstanceName" (i.e. clarkr-testmachine\SQLEXPRESS)
            // we should handle both...if sqlInstanceName isn't specified, we assume default.
            string[] names = builder.DataSource.Split(new char[] { '\\' });
            if (names.Length > 0)
            {
                _serverHostName = names[0].ToUpperInvariant();

                if (names.Length > 1)
                {
                    _sqlInstanceName = names[1];
                }
            }
            else
            {
                throw new ArgumentException("'Data Source' must have at least the name of the server where database is running", "connString");
            }

            _databaseName = builder.InitialCatalog;
            _connString = builder.ConnectionString; // capture the connection string that we've modified

            //Log.TraceInternal("[SqlInstance] Database: " + databaseName + " Connection String: " + connString);
        }

        ~SqlInstance()
        {
            Dispose(false);
        }

        #endregion ctors

        #region properties
        public string ServerHostName
        {
            get { return _serverHostName; }
        }

        public string InstanceName
        {
            get { return _sqlInstanceName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public bool IsStarted
        {
            get { return true; }
        }

        public bool IsLocal
        {
            get
            {
                string localMachineName = "localhost"; //PartialTrustDNS.GetHostName().ToUpperInvariant();
                return (localMachineName == this.ServerHostName);
            }
        }

        public bool IsExpressEdition
        {
            // sql: select serverproperty('edition')
            // result = 'Express Edition'
            get { return (this.InstanceName == "SQLEXPRESS"); }
        }

        public bool IsDefault
        {
            get
            {
                // sql: select serverProperty('InstanceName')
                // result = null if default 
                bool bRet = IsExpressEdition;
                if (bRet == false)
                {
                    // check further
                    if (this.InstanceName == "")
                    {
                        bRet = true;
                    }
                }
                return bRet;
            }
        }

        public SqlConnection MasterConnection
        {
            get
            {
                if (_masterConnection == null || _masterConnection.State == ConnectionState.Closed)
                {
                    _masterConnection = new SqlConnection(this.GenerateMasterConnectionString());
                    // PartialTrustSQLConnect.Open(this.masterConnection);
                }

                return _masterConnection;
            }
        }

        public SqlConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    _connection = new SqlConnection(this.GenerateConnectionString());
                    // PartialTrustSQLConnect.Open(connection);
                }

                return _connection;
            }
        }
        #endregion properties

        #region public methods

        public void SetDatabase(string databaseName)
        {
            string sql = "Use [{0}]";
            string sqlScript = string.Format(sql, databaseName);
            _databaseName = databaseName;

            // if connection is null, the next time it is opened will by default set to db
            if (_connection != null)
            {
                this.ExecuteSqlString(sqlScript);
            }
        }

        public string GenerateConnectionString()
        {
            // If no database is set, generate connection string to master database
            return GenerateConnectionStringInternal(string.IsNullOrEmpty(this.DatabaseName) ? "master" : this.DatabaseName);
        }

        public string GenerateMasterConnectionString()
        {
            return GenerateConnectionStringInternal("master");
        }

        private string GenerateConnectionStringInternal(string dbName)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connString);
            builder.InitialCatalog = dbName;
            // builder.AsynchronousProcessing = builder.InitialCatalog == "master" ? false : true;
            return builder.ConnectionString;
        }

        public void ExecuteSqlFile(string scriptFilePath)
        {
            //Log.TraceInternal("[SqlInstance] SqlScript: " + scriptFilePath);
            //string sqlScriptInString = PartialTrustFile.ReadAllText(scriptFilePath);
            //ExecuteSqlString(sqlScriptInString);
        }

        public void ExecuteSqlString(string script, SqlConnection conn)
        {
            SqlCommand sqlCommand = null;
            try
            {
                sqlCommand = new SqlCommand("", conn);
                script += Environment.NewLine; //Append new line in case the last statement is 'GO'
                //@"\r\nGO\r\n|\r\nGO$",
                string[] commandSegments = Regex.Split(script, @"^\s*GO\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                foreach (string command in commandSegments)
                {
                    if (!string.IsNullOrEmpty(command))
                    {
                        sqlCommand.CommandText = command;
                        // PartialTrustSQLCommand.ExecuteNonQuery(sqlCommand);
                    }
                }
            }
            catch (SqlException) // jasonv - approved; specific, logs useful data, rethrows
            {
                if (sqlCommand != null)
                {
                    //Log.TraceInternal(string.Format("[SqlInstance] Exception thrown while running sql command: {0}", sqlCommand.CommandText));
                }

                throw;
            }
        }

        public void ExecuteSqlString(string script)
        {
            ExecuteSqlString(script, this.Connection);
        }

        public int ExecuteSqlStringScalarCount(string script)
        {
            return ExecuteSqlStringScalarCount(script, this.Connection);
        }

        public bool CheckExistObjectInDatabase(SqlObjectType type, string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentException("Object name can not be null or empty", "objectName");
            }

            string format = "SELECT count(*) FROM sys.objects WHERE type = '{0}' and name = N'{1}'";
            string sqlScript = string.Format(format, SqlObjectTypeToString(type), objectName);

            int count = ExecuteSqlStringScalarCount(sqlScript);

            if (count != 1)
            {
                //Log.TraceInternal("[SqlInstance] CheckExist count: " + count + "; SqlScript: " + sqlScript);
                return false;
            }

            return true;
        }

        // Using this method is more efficient than checking for each object with CheckExistObjectInDatabase
        public bool CheckObjectsExistInDatabase(SqlObjectType type, ICollection<string> objectNames, bool checkCount)
        {
            if (objectNames == null)
            {
                throw new ArgumentNullException("objectNames");
            }

            string format = "SELECT * FROM sys.objects WHERE type = '{0}'";
            string sqlScript = string.Format(format, SqlObjectTypeToString(type));

            List<string> results = ExecuteSqlStringQuery(sqlScript, this.Connection);

            HashSet<string> resultSet = new HashSet<string>(results);

            bool ret = true;

            foreach (string name in objectNames)
            {
                if (!resultSet.Contains(name))
                {
                    //Log.TraceInternal("[SqlInstance] Could not find sql object: name='{0}', type='{1}'", name, type.ToString());

                    ret = false;
                }
            }

            if (checkCount && results.Count != objectNames.Count)
            {
                //Log.TraceInternal("[SqlInstance] CheckCount mismatch: expected={0}, actual={1}", objectNames.Count, results.Count);

                ret = false;
            }

            return ret;
        }

        public bool CheckNumberOfObjectsInDatabase(SqlObjectType type, int expected)
        {
            string format = "SELECT count(*) FROM sys.objects WHERE type = '{0}'";
            string sqlScript = string.Format(format, SqlObjectTypeToString(type));

            int count = ExecuteSqlStringScalarCount(format);

            if (count != expected)
            {
                //Log.TraceInternal("[SqlInstance] CheckNumberObjects mismatch, count: " + count + "; Expected: " + expected);
                return false;
            }

            return true;
        }

        public bool CheckExistDatabase(string name)
        {
            string format = "SELECT count(*) FROM sys.databases WHERE name = N'{0}'";
            string sqlScript = string.Format(format, name);

            int count = this.ExecuteSqlStringScalarCount(sqlScript, this.MasterConnection);

            if (count != 1)
            {
                //Log.TraceInternal("[SqlInstance] CheckExist count: " + count + "; SqlScript: " + sqlScript);
            }

            return count == 1;
        }

        public void DropDatabase()
        {
            if (string.Compare(this.DatabaseName, "master", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // don't let people accidentally drop the master database
            }
            else
            {
                // Prepare to drop database:
                // 1) Generate scripts
                // 2) Set it to single user with rollback
                // 3) Switch to master
                // 4) Close the connection to the database we are about to drop

                string sqlDropDatabase = "DROP DATABASE [{0}]";
                string sqlScriptDropDatabase = string.Format(sqlDropDatabase, _databaseName);

                string sqlSingleUser = "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                string sqlScriptSingleUser = string.Format(sqlSingleUser, _databaseName);

                this.SetDatabase("master");
                this.CloseConnection();


                // Now we can drop the database
                this.ExecuteSqlString(sqlScriptSingleUser);
                this.ExecuteSqlString(sqlScriptDropDatabase);
            }
        }

        public void CreateDatabase(string databaseName)
        {
            string sql = "Create Database [{0}]"; // Go";
            string sqlScript = string.Format(sql, databaseName);
            ExecuteSqlString(sqlScript, this.MasterConnection);
            SetDatabase(databaseName);
        }

        public void CreateLogin(string name)
        {
            // e.g. userName = redmond\_kwtest1
            // encapsulates functionality for CreateLogin / sp_grantlogin
            string sql = "CREATE LOGIN [{0}] FROM WINDOWS";
            string sqlScript = string.Format(sql, name);
            ExecuteSqlString(sqlScript, this.MasterConnection);
        }

        public void DropLogin(string name)
        {
            // e.g. userName = redmond\_kwtest1
            // encapsulates functionality for Drop login / sp_droplogin
            string sql = "DROP LOGIN [{0}]";
            string sqlScript = string.Format(sql, name);
            ExecuteSqlString(sqlScript, this.MasterConnection);
        }

        public bool CheckExistLogin(string name)
        {
            string sql = "SELECT count(*) from syslogins where name = N'{0}'";
            string sqlScript = string.Format(sql, name);

            int count = this.ExecuteSqlStringScalarCount(sqlScript, this.MasterConnection);

            if (count != 1)
            {
                //Log.TraceInternal("[SqlInstance] CheckExist count: " + count + "; SqlScript: " + sqlScript);
            }

            return count == 1;
        }

        public void CreateUser(string name, string loginName)
        {
            // e.g. name = keith loginName = redmond\keith 
            // encapsulates functionality for Create User / sp_grantdbaccess
            // it is created in current database
            string sql = "CREATE USER [{0}] FOR LOGIN [{1}]";
            string sqlScript = string.Format(sql, name, loginName);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public void DropUser(string name)
        {
            // e.g. name _kwtest1
            // encapsulates functionality for Drop user / sp_dropdbaccess 
            string sql = "DROP USER [{0}]";
            string sqlScript = string.Format(sql, name);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public bool CheckExistUser(string name)
        {
            string sql = "SELECT count(*) from sysusers where name = N'{0}'";
            string sqlScript = string.Format(sql, name);

            int count = this.ExecuteSqlStringScalarCount(sqlScript, this.Connection);

            if (count != 1)
            {
                //Log.TraceInternal("[SqlInstance] CheckExist count: " + count + "; SqlScript: " + sqlScript);
            }

            return count == 1;
        }

        public void AddUserToRole(string user, string roleName)
        {
            // e.g. name = keith role = persistenceUsers 
            // it is created in current database
            string sql = "sp_addrolemember [{0}], [{1}]";
            string sqlScript = string.Format(sql, roleName, user);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public void DropUserFromRole(string user, string roleName)
        {
            string sql = "sp_droprolemember [{0}], [{1}]";
            string sqlScript = string.Format(sql, roleName, user);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public bool CheckExistUserInRole(string user, string roleName)
        {
            string sql = @"select COUNT(*) from sys.database_role_members 
                           where role_principal_id = (select principal_id from sys.database_principals where name = '{0}')
                           and member_principal_id = (select principal_id from sys.database_principals where name = '{1}')";
            string sqlScript = string.Format(sql, roleName, user);

            int count = this.ExecuteSqlStringScalarCount(sqlScript, this.Connection);

            if (count != 1)
            {
                //Log.TraceInternal("[SqlInstance] CheckExist count: " + count + "; SqlScript: " + sqlScript);
            }

            return count == 1;
        }

        public void AddLoginToFixedRole(string login, string roleName)
        {
            // sp_addsrvrolemember 'Corporate\HelenS', 'sysadmin'
            // it is created in current database
            string sql = "sp_addsrvrolemember [{0}], [{1}]";
            string sqlScript = string.Format(sql, login, roleName);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public void DropLoginFromFixedRole(string login, string roleName)
        {
            // sp_dropsrvrolemember 'JackO', 'sysadmin'
            string sql = "sp_dropsrvrolemember [{0}], [{1}]";
            string sqlScript = string.Format(sql, login, roleName);
            ExecuteSqlString(sqlScript, this.Connection);
        }

        public bool CheckExistUserInFixedRole(string user, string roleName)
        {
            throw (new NotImplementedException("CheckExistUserInFixedRole"));
            //return true;
        }
        #endregion public methods

        #region private methods
        private void CloseConnection()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        private void CloseMasterConnection()
        {
            if (_masterConnection != null)
            {
                _masterConnection.Close();
                _masterConnection = null;
            }
        }

        private int ExecuteSqlStringScalarCount(string script, SqlConnection conn)
        {
            // temporary fix: reopen connection after it is closed, fail after 3 tries
            int iRet = 0;
            int tryCount = 0;
            while (true)
            {
                try
                {
                    using (SqlCommand command = new SqlCommand("", conn))
                    {
                        command.CommandText = script;
                        // iRet = (int)PartialTrustSQLCommand.ExecuteScalar(command);
                    }
                    break;
                }
                catch (SqlException e) // jasonv - approved; specific, commented, rethrows after retries
                {
                    //Log.TraceInternal(e.Message);
                    //Log.TraceInternal("[SqlInstance] After exception Connection state is " + conn.State);
                    if (tryCount++ < 3)
                    {
                        // PartialTrustSQLConnect.Open(conn);
                    }
                    else throw;
                }
            }
            return iRet;
        }

        private List<string> ExecuteSqlStringQuery(string script, SqlConnection conn)
        {
            using (SqlCommand command = new SqlCommand(script, conn))
                //using(SqlDataReader reader = PartialTrustSQLCommand.ExecuteReader(command))
                //{
                //    List<string> results = new List<string>(reader.FieldCount);

                //    while(reader.Read())
                //    {
                //        results.Add(reader[0].ToString());
                //    }

                //    return results;
                //}
                return new List<string>();
        }

        private string SqlObjectTypeToString(SqlObjectType type)
        {
            switch (type)
            {
                case SqlObjectType.Table:
                    return "U";
                case SqlObjectType.View:
                    return "V";
                case SqlObjectType.StoredProcedure:
                    return "P";
                default:
                    throw new ArgumentException("SqlObjectType is not supported", "type");
            }
        }

        #endregion private methods

        #region override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name: " + this.InstanceName);
            sb.AppendLine("ServerName: " + this.ServerHostName);
            sb.AppendLine("DbName: " + this.DatabaseName);
            sb.AppendLine("IsStarted: " + this.IsStarted);
            sb.AppendLine("IsLocal: " + this.IsLocal);
            sb.AppendLine("IsExpressEdition: " + this.IsExpressEdition);
            sb.AppendLine("IsDefault: " + this.IsDefault);
            sb.AppendLine("ConnStr: " + this.GenerateConnectionString());
            return sb.ToString();
        }

        #endregion override methods

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing)
                {
                    this.CloseConnection();
                    this.CloseMasterConnection();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
