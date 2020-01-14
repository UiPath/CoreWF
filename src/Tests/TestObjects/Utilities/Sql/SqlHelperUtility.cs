// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace Test.Common.TestObjects.Utilities.Sql
{
    public static class SqlHelperUtility
    {
        public static SqlInstance CreateCustomDatabase(string DatabaseName)
        {
            SqlInstance instance = new SqlInstance();
            instance.CreateDatabase(DatabaseName);
            instance.SetDatabase(DatabaseName);
            DatabaseConfiguration dbConfig = DatabaseConfigurations.SqlPersistenceConfiguration(InstanceStoreVersion.Version45);
            instance.ExecuteSqlFile(dbConfig.CreateSchema);
            instance.ExecuteSqlFile(dbConfig.CreateLogic);
            return instance;
        }
    }
}
