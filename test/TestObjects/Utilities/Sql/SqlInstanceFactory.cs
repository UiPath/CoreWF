// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Win32;

namespace Test.Common.TestObjects.Utilities.Sql
{
    public class SqlInstanceFactory
    {
        #region variables
        private string _machineName;
        private bool? _expressEditionFilter;
        private bool? _defaultInstanceFilter;
        private bool? _localMachineFilter;
        #endregion variables

        #region ctors & factory methods

        private SqlInstanceFactory(string machineName, bool? expressEditionFilter, bool? defaultInstanceFilter)
        {
            this.ExpressEditionFilter = expressEditionFilter;
            this.DefaultInstanceFilter = defaultInstanceFilter;
            this.MachineName = machineName;
        }

        #endregion ctors & factory methods

        #region public methods
        public string MachineName
        {
            set
            {
                // local can be ., localhost, or nothing
                string localMachineName = "localhost"; //PartialTrustDNS.GetHostName().ToUpperInvariant();
                if ((string.IsNullOrEmpty(value)) ||
                    (value.ToUpperInvariant().CompareTo("LOCALHOST") == 0) ||
                    (value.CompareTo(".") == 0) ||
                    (value.ToUpperInvariant().CompareTo(localMachineName) == 0))
                {
                    // set to local machine
                    _machineName = "localhost"; //PartialTrustDNS.GetHostName();
                    _localMachineFilter = true;
                }
                else
                {
                    _machineName = value.ToUpperInvariant();
                    _localMachineFilter = false;
                }
            }
            get
            {
                return _machineName;
            }
        }

        public bool? LocalServerFilter
        {
            // read only set by when machine name is set
            get { return _localMachineFilter; }
        }

        public bool? DefaultInstanceFilter
        {
            set { _defaultInstanceFilter = value; }
            get { return _defaultInstanceFilter; }
        }

        public bool? ExpressEditionFilter
        {
            set { _expressEditionFilter = value; }
            get { return _expressEditionFilter; }
        }

        // this method doesn't work on ARM/SQLAzure, use GetDefaultSqlInstance()
        public static List<SqlInstance> RetrieveInstances()
        {
            // use default instance on localhost
            return (SqlInstanceFactory.RetrieveInstances(".", null, true));
        }

        // this method doesn't work on ARM/SQLAzure, use GetDefaultSqlInstance()
        public static List<SqlInstance> RetrieveInstances(string machineName, bool? expressEditionFilter, bool? defaultInstanceFilter)
        {
            SqlInstanceFactory sqlIF = new SqlInstanceFactory(machineName, expressEditionFilter, defaultInstanceFilter);
            return sqlIF.GetInstances();
        }

        // this method doesn't work on ARM/SQLAzure, use GetDefaultSqlInstance()
        public List<SqlInstance> GetInstances()
        {
            if (this.LocalServerFilter == true)
            {
                return (this.GetInstanceFromLocal());
            }

            //string colServerName = "ServerName";
            //string colInstanceName = "InstanceName";
            //string colVersion = "Version";

            //SqlDataSourceEnumerator instance = SqlDataSourceEnumerator.Instance;
            //System.Data.DataTable table = instance.GetDataSources();
            //List<SqlInstance> sqlInstances = new List<SqlInstance>(3);
            //foreach (System.Data.DataRow row in table.Rows)
            //{
            //    string instanceName = row[colInstanceName] == DBNull.Value ? "" : (string)row[colInstanceName];
            //    string serverName = row[colServerName] == DBNull.Value ? "" : (string)row[colServerName];
            //    string version = row[colVersion] == DBNull.Value ? "" : (string)row[colVersion];
            //    SqlInstance sqlInstance = new SqlInstance(instanceName, serverName, version);

            //    if (IsMeetingFilterCriteria(sqlInstance))
            //    {
            //        sqlInstances.Add(sqlInstance);
            //    }
            //}
            return null;
        }

        // Check if a local SQL server is installed and running
        public static bool IsSqlRunning()
        {
            //Log.TraceInternal("Checking for SQL on the local machine");
            return SqlInstanceFactory.RetrieveInstances().Count > 0;
        }

        // Check if a local SQL server is installed and running on a particular machine
        public static bool IsSqlRunning(string machineName)
        {
            //Log.TraceInternal("Checking for a specific SQL on the machine");
            return SqlInstanceFactory.RetrieveInstances(machineName, null, true).Count > 0;
        }

        public static SqlInstance GetDefaultSqlInstance()
        {
            //if (TestParameters.IsSqlAzureRun || TestParameters.IsArmRun)
            //{
            //    //Log.TraceInternal("Detected SQL Azure or ARM run, using SqlPersistenceConnectionString (" + TestParameters.SqlPersistenceConnectionString + ") directly");
            //    return new SqlInstance(TestParameters.SqlPersistenceConnectionString);
            //}

            SqlInstance defaultSqlInstance = null;
            List<SqlInstance> sqlInstances = SqlInstanceFactory.RetrieveInstances("localhost", null, true);
            if (sqlInstances != null && sqlInstances.Count > 0)
            {
                defaultSqlInstance = sqlInstances[0];
            }
            return defaultSqlInstance;
        }

        public static string GetDefaultSqlServerName()
        {
            //if (TestParameters.IsSqlAzureRun || TestParameters.IsArmRun)
            //{
            //    //As this is an azure run extracting default sql Server Name from ConnectionString
            //    string connString = TestParameters.SqlPersistenceConnectionString;
            //    SqlConnection connection = new SqlConnection(connString);
            //    return connection.DataSource.ToString();
            //}

            SqlInstance defaultSqlInstance = GetDefaultSqlInstance();
            return (defaultSqlInstance != null) ? defaultSqlInstance.MasterConnection.DataSource : "localhost";//PartialTrustEnvironment.GetMachineName();
        }

        #endregion public methods

        #region private methods
        private List<SqlInstance> GetInstanceFromLocal()
        {
            // will workaround picking up info from registry 

            //[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL]
            //[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL]
            //For all other Sql installs we expect this value: "MSSQLSERVER"="MSSQL10.MSSQLSERVER"
            //For SqlExpress installs we expect this value: "SQLEXPRESS"="MSSQL.1"

            List<SqlInstance> sqlInstances;
            List<SqlInstance> sqlInstancesWowNode;

            sqlInstances = GetInstanceFromRegistryKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL" /*, RegistryView.Registry64*/);

            // x86 SQL can be installed on 64bit OS so we need to check the WOW hive. This check is NOT to handle the scenario
            // where the Test Process is running as WOW. That is handled in the GetInstanceFromRegistryKey function.
            sqlInstancesWowNode = GetInstanceFromRegistryKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL" /*, RegistryView.Default*/);

            if (sqlInstancesWowNode.Count > 0)
            {
                foreach (SqlInstance instance in sqlInstancesWowNode)
                {
                    sqlInstances.Add(instance);
                }
            }

            if (sqlInstances.Count == 0)
            {
                //Log.TraceInternal("[SqlInstanceFactory] Can't find local sql instance key");
            }

            return sqlInstances;
        }

        private List<SqlInstance> GetInstanceFromRegistryKey(string registryKey/*, RegistryView registryView*/)
        {
            List<SqlInstance> sqlInstances = new List<SqlInstance>();
            //using (RegistryKey baseKey = PartialTrustRegistry.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            //{
            //    using (RegistryKey instanceKey = PartialTrustRegistry.OpenSubKey(baseKey, registryKey))
            //    {
            //        if (instanceKey != null)
            //        {
            //            string[] values = PartialTrustRegistry.GetValueNames(instanceKey);
            //            foreach (string s in values)
            //            {
            //                string instanceName = s;
            //                if (instanceName == "MSSQLSERVER")
            //                {
            //                    // If we find a value name of MSSQLSERVER (standard or enterprise), then we assume the instance is the
            //                    // default sql server instance. The default instance always has a blank name.
            //                    instanceName = "";
            //                }
            //                SqlInstance sqlInstance = new SqlInstance(this.MachineName, instanceName);
            //                if (IsMeetingFilterCriteria(sqlInstance))
            //                {
            //                    sqlInstances.Add(sqlInstance);
            //                }
            //                else
            //                {
            //                    // The SqlInstance found did not match the filter criteria so dispose of it.
            //                    sqlInstance.Dispose();
            //                }
            //            }
            //        }
            //    }
            //}
            return sqlInstances;
        }

        private bool IsMeetingFilterCriteria(SqlInstance sqlInstance)
        {
            bool bRet = true;

            do // do while(false) to allow break - readability
            {
                if (string.Compare(this.MachineName, sqlInstance.ServerHostName, true) != 0)
                {
                    bRet = false;
                    break;
                }

                if ((_defaultInstanceFilter != null) && (sqlInstance.IsDefault != _defaultInstanceFilter))
                {
                    bRet = false;
                    break;
                }

                if ((_expressEditionFilter != null) && (sqlInstance.IsExpressEdition != _expressEditionFilter))
                {
                    bRet = false;
                    break;
                }
            }
            while (false);
            return bRet;
        }
        #endregion private methods
    }
}
