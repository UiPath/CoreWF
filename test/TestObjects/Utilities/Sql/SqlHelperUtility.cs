// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

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
