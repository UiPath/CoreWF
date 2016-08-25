// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Utilities;

namespace Test.Common.TestObjects.Utilities.Sql
{
    public enum InstanceStoreVersion
    {
        Version35,
        Version40,
        Version45
    }

    // Provides singletons of each persistence store database configuration
    public static class DatabaseConfigurations
    {
        private static readonly string s_scriptDirectoryPath = string.Empty; // DirectoryAssistance.GetSQLFilesDirectory();
        private static readonly string s_scriptDirectoryPath40 = string.Empty; // Path.GetDirectoryName(PartialTrustPath.GetAssemblyLocation(typeof(DatabaseConfigurations).Assembly));
        private static readonly DatabaseConfiguration s_sqlPersistence35Config = InitSqlPersistenceConfiguration(InstanceStoreVersion.Version35);
        private static readonly DatabaseConfiguration s_sqlPersistence40Config = InitSqlPersistenceConfiguration(InstanceStoreVersion.Version40);
        private static readonly DatabaseConfiguration s_sqlPersistence45Config = InitSqlPersistenceConfiguration(InstanceStoreVersion.Version45);
        private static readonly DatabaseConfiguration s_sqlRefProviderConfig = InitTestReferenceProviderConfiguration();

        public static DatabaseConfiguration SqlPersistenceConfiguration(InstanceStoreVersion DBVersion)
        {
            switch (DBVersion)
            {
                case InstanceStoreVersion.Version35:
                    return s_sqlPersistence35Config;
                case InstanceStoreVersion.Version40:
                    return s_sqlPersistence40Config;
                case InstanceStoreVersion.Version45:
                    return s_sqlPersistence45Config;
                default:
                    return s_sqlPersistence45Config;
            }
        }

        public static DatabaseConfiguration TestReferenceProviderConfiguration()
        {
            return s_sqlRefProviderConfig;
        }

        private static DatabaseConfiguration InitTestReferenceProviderConfiguration()
        {
            string[] refStoreTables =
            {
                "InstanceStore",
                "KeyStore",
                "OwnerStore",
                "PropertyStore"
            };

            string[] refStoreStoredProcs =
            {
                "CreateOwner",
                "GetMetadata",
                "LockInstance",
                "LoadInstance",
                "PersistInstance",
                "UnlockInstance",
                "WriteMetadata",
                "UnAssociateKey",
                "CompleteKey",
                "CompleteInstance",
                "AssociateKey",
                "FindAssociatedKey",
                "DeleteOwner"
            };

            return new DatabaseConfiguration()
            {
                CreateSchema = "TestRefInstanceStore_Schema.sql",
                CreateLogic = String.Empty,
                TableNames = new List<string>(refStoreTables),
                StoredProcedureNames = new List<string>(refStoreStoredProcs)
            };
        }

        private static DatabaseConfiguration InitSqlPersistenceConfiguration(InstanceStoreVersion DBVersion)
        {
            switch (DBVersion)
            {
                case (InstanceStoreVersion.Version40):
                    string[] sqlInstanceStore40Tables =
                    {
                        "InstanceMetadataChangesTable",
                        "InstancePromotedPropertiesTable",
                        "InstancesTable",
                        "KeysTable",
                        "LockOwnersTable",
                        "RunnableInstancesTable",
                        "ServiceDeploymentsTable",
                        "SqlWorkflowInstanceStoreVersionTable"
                    };

                    string[] sqlInstanceStore40StoredProcs =
                    {
                        "AssociateKeys",
                        "CompleteKeys",
                        "CreateInstance",
                        "CreateLockOwner",
                        "CreateServiceDeployment",
                        "DeleteInstance",
                        "DeleteLockOwner",
                        "DetectRunnableInstances",
                        "ExtendLock",
                        "FreeKeys",
                        "GetActivatableWorkflowsActivationParameters",
                        "InsertPromotedProperties",
                        "InsertRunnableInstanceEntry",
                        "LoadInstance",
                        "LockInstance",
                        "RecoverInstanceLocks",
                        "SaveInstance",
                        "TryLoadRunnableInstance",
                        "UnlockInstance",
                    };

                    string[] sqlInstanceStore40Roles =
                    {
                        "Microsoft.CoreWf.DurableInstancing.InstanceStoreObservers",
                        "Microsoft.CoreWf.DurableInstancing.InstanceStoreUsers"
                    };

                    return new DatabaseConfiguration()
                    {
                        CreateSchema = Path.Combine(s_scriptDirectoryPath40, "SqlWorkflowInstanceStoreSchema40.sql"),
                        CreateLogic = Path.Combine(s_scriptDirectoryPath40, "SqlWorkflowInstanceStoreLogic40.sql"),
                        DropSchema = Path.Combine(s_scriptDirectoryPath, "DropSqlWorkflowInstanceStoreSchema.sql"),
                        DropLogic = Path.Combine(s_scriptDirectoryPath, "DropSqlWorkflowInstanceStoreLogic.sql"),
                        UpgradeSchema = Path.Combine(s_scriptDirectoryPath, "SqlWorkflowInstanceStoreSchemaUpgrade.sql"),
                        TableNames = new List<string>(sqlInstanceStore40Tables),
                        StoredProcedureNames = new List<string>(sqlInstanceStore40StoredProcs),
                        Roles = new List<string>(sqlInstanceStore40Roles)
                    };

                case (InstanceStoreVersion.Version35):
                    string[] legacySqlPPTables = { "InstanceData" };

                    string[] legacySqlPPStoredProcs = {
                        "DeleteInstance",
                        "InsertInstance",
                        "LoadInstance",
                        "UnlockInstance",
                        "UpdateInstance" };

                    string[] legacySqlPPRoles = { "persistenceUsers" };

                    return new DatabaseConfiguration()
                    {
                        CreateSchema = Path.Combine(s_scriptDirectoryPath, "SqlPersistenceProviderSchema.sql"),
                        CreateLogic = Path.Combine(s_scriptDirectoryPath, "SqlPersistenceProviderLogic.sql"),
                        DropSchema = Path.Combine(s_scriptDirectoryPath, "DropSqlPersistenceProviderSchema.sql"),
                        DropLogic = Path.Combine(s_scriptDirectoryPath, "DropSqlPersistenceProviderLogic.sql"),
                        UpgradeSchema = Path.Combine(s_scriptDirectoryPath, "SqlWorkflowInstanceStoreSchemaUpgrade.sql"),
                        TableNames = new List<string>(legacySqlPPTables),
                        StoredProcedureNames = new List<string>(legacySqlPPStoredProcs),
                        Roles = new List<string>(legacySqlPPRoles)
                    };
                default:
                    string[] sqlInstanceStoreTables =
                    {
                        "InstanceMetadataChangesTable",
                        "InstancePromotedPropertiesTable",
                        "InstancesTable",
                        "KeysTable",
                        "LockOwnersTable",
                        "RunnableInstancesTable",
                        "ServiceDeploymentsTable",
                        "SqlWorkflowInstanceStoreVersionTable",
                        "DefinitionIdentityTable",
                        "IdentityOwnerTable",
                    };

                    string[] sqlInstanceStoreStoredProcs =
                    {
                        "AssociateKeys",
                        "CompleteKeys",
                        "CreateInstance",
                        "CreateLockOwner",
                        "CreateServiceDeployment",
                        "DeleteInstance",
                        "DeleteLockOwner",
                        "DetectRunnableInstances",
                        "ExtendLock",
                        "FreeKeys",
                        "GetActivatableWorkflowsActivationParameters",
                        "InsertPromotedProperties",
                        "InsertRunnableInstanceEntry",
                        "LoadInstance",
                        "LockInstance",
                        "RecoverInstanceLocks",
                        "SaveInstance",
                        "TryLoadRunnableInstance",
                        "UnlockInstance",
                        "GetWorkflowInstanceStoreVersion",
                        "InsertDefinitionIdentity"
                    };

                    string[] sqlInstanceStoreRoles =
                    {
                        "Microsoft.CoreWf.DurableInstancing.InstanceStoreObservers",
                        "Microsoft.CoreWf.DurableInstancing.InstanceStoreUsers"
                    };

                    return new DatabaseConfiguration()
                    {
                        CreateSchema = Path.Combine(s_scriptDirectoryPath, "SqlWorkflowInstanceStoreSchema.sql"),
                        CreateLogic = Path.Combine(s_scriptDirectoryPath, "SqlWorkflowInstanceStoreLogic.sql"),
                        DropSchema = Path.Combine(s_scriptDirectoryPath, "DropSqlWorkflowInstanceStoreSchema.sql"),
                        DropLogic = Path.Combine(s_scriptDirectoryPath, "DropSqlWorkflowInstanceStoreLogic.sql"),
                        UpgradeSchema = Path.Combine(s_scriptDirectoryPath, "SqlWorkflowInstanceStoreSchemaUpgrade.sql"),
                        TableNames = new List<string>(sqlInstanceStoreTables),
                        StoredProcedureNames = new List<string>(sqlInstanceStoreStoredProcs),
                        Roles = new List<string>(sqlInstanceStoreRoles)
                    };
            }
        }
    }
}
