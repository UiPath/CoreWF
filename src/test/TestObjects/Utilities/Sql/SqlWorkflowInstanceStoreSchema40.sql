set ansi_nulls on
set quoted_identifier on
set nocount on
go

if not exists( select 1 from [dbo].[sysusers] where name=N'CoreWf.DurableInstancing.InstanceStoreUsers' and issqlrole=1 )
	create role [CoreWf.DurableInstancing.InstanceStoreUsers]
go

if not exists( select 1 from [dbo].[sysusers] where name=N'CoreWf.DurableInstancing.WorkflowActivationUsers' and issqlrole=1 )
	create role [CoreWf.DurableInstancing.WorkflowActivationUsers]
go

if not exists( select 1 from [dbo].[sysusers] where name=N'CoreWf.DurableInstancing.InstanceStoreObservers' and issqlrole=1 )
	create role [CoreWf.DurableInstancing.InstanceStoreObservers]
go

if not exists (select * from sys.schemas where name = N'CoreWf.DurableInstancing')
	exec ('create schema [CoreWf.DurableInstancing]')
go

if exists (select * from sys.views where object_id = object_id(N'[CoreWf.DurableInstancing].[InstancePromotedProperties]'))
      drop view [CoreWf.DurableInstancing].[InstancePromotedProperties]
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[InstancesTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[InstancesTable]
go

create table [CoreWf.DurableInstancing].[InstancesTable]
(
	[Id] uniqueidentifier not null,
	[SurrogateInstanceId] bigint identity not null,
	[SurrogateLockOwnerId] bigint null,
	[PrimitiveDataProperties] varbinary(max) default null,
	[ComplexDataProperties] varbinary(max) default null,
	[WriteOnlyPrimitiveDataProperties] varbinary(max) default null,
	[WriteOnlyComplexDataProperties] varbinary(max) default null,
	[MetadataProperties] varbinary(max) default null,
	[DataEncodingOption] tinyint default 0,
	[MetadataEncodingOption] tinyint default 0,
	[Version] bigint not null,
	[PendingTimer] datetime null,
	[CreationTime] datetime not null,
	[LastUpdated] datetime default null,
	[WorkflowHostType] uniqueidentifier null,
	[ServiceDeploymentId] bigint null,
	[SuspensionExceptionName] nvarchar(450) default null,
	[SuspensionReason] nvarchar(max) default null,
	[BlockingBookmarks] nvarchar(max) default null,
	[LastMachineRunOn] nvarchar(450) default null,
	[ExecutionStatus] nvarchar(450) default null,
	[IsInitialized] bit default 0,
	[IsSuspended] bit default 0,
	[IsReadyToRun] bit default 0,
	[IsCompleted] bit default 0
)
go

create unique clustered index [CIX_InstancesTable]
	on [CoreWf.DurableInstancing].[InstancesTable] ([SurrogateInstanceId])
	with (allow_page_locks = off)
go

create unique nonclustered index [NCIX_InstancesTable_Id]
	on [CoreWf.DurableInstancing].[InstancesTable] ([Id])
	include ([Version], [SurrogateLockOwnerId], [IsCompleted])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_InstancesTable_SurrogateLockOwnerId]
	on [CoreWf.DurableInstancing].[InstancesTable] ([SurrogateLockOwnerId])
	with (allow_page_locks = off)
go

create nonclustered index NCIX_InstancesTable_LastUpdated
	on [CoreWf.DurableInstancing].[InstancesTable] ([LastUpdated])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_InstancesTable_CreationTime]
	on [CoreWf.DurableInstancing].[InstancesTable] ([CreationTime])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_InstancesTable_SuspensionExceptionName]
	on [CoreWf.DurableInstancing].[InstancesTable] ([SuspensionExceptionName])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_InstancesTable_ServiceDeploymentId]
	on [CoreWf.DurableInstancing].[InstancesTable] ([ServiceDeploymentId])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_InstancesTable_WorkflowHostType]
	on [CoreWf.DurableInstancing].[InstancesTable] ([WorkflowHostType])
	with (allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[RunnableInstancesTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[RunnableInstancesTable]
go

create table [CoreWf.DurableInstancing].[RunnableInstancesTable]
(
	[SurrogateInstanceId] bigint not null,		
	[WorkflowHostType] uniqueidentifier null,
	[ServiceDeploymentId] bigint null,
	[RunnableTime] datetime not null
)
go

create unique clustered index [CIX_RunnableInstancesTable_SurrogateInstanceId]
	on [CoreWf.DurableInstancing].[RunnableInstancesTable] ([SurrogateInstanceId])
	with (ignore_dup_key = on, allow_page_locks = off)
go

create nonclustered index [NCIX_RunnableInstancesTable]
	on [CoreWf.DurableInstancing].[RunnableInstancesTable] ([WorkflowHostType], [RunnableTime])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_RunnableInstancesTable_RunnableTime]
	on [CoreWf.DurableInstancing].[RunnableInstancesTable] ([RunnableTime]) include ([WorkflowHostType], [ServiceDeploymentId])
	with (allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[KeysTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[KeysTable]
go

create table [CoreWf.DurableInstancing].[KeysTable]
(
	[Id] uniqueidentifier not null,
	[SurrogateKeyId] bigint identity not null,
	[SurrogateInstanceId] bigint null,
	[EncodingOption] tinyint null,
	[Properties] varbinary(max) null,
	[IsAssociated] bit
) 
go

create unique clustered index [CIX_KeysTable]
	on [CoreWf.DurableInstancing].[KeysTable] ([Id])	
	with (ignore_dup_key = on, allow_page_locks = off)
go

create nonclustered index [NCIX_KeysTable_SurrogateInstanceId]
	on [CoreWf.DurableInstancing].[KeysTable] ([SurrogateInstanceId])
	with (allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[LockOwnersTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[LockOwnersTable]
go

create table [CoreWf.DurableInstancing].[LockOwnersTable]
(
	[Id] uniqueidentifier not null,
	[SurrogateLockOwnerId] bigint identity not null,
	[LockExpiration] datetime not null,
	[WorkflowHostType] uniqueidentifier null,
	[MachineName] nvarchar(128) not null,
	[EnqueueCommand] bit not null,
	[DeletesInstanceOnCompletion] bit not null,
	[PrimitiveLockOwnerData] varbinary(max) default null,
	[ComplexLockOwnerData] varbinary(max) default null,
	[WriteOnlyPrimitiveLockOwnerData] varbinary(max) default null,
	[WriteOnlyComplexLockOwnerData] varbinary(max) default null,
	[EncodingOption] tinyint default 0
)
go

create unique clustered index [CIX_LockOwnersTable]
	on [CoreWf.DurableInstancing].[LockOwnersTable] ([SurrogateLockOwnerId])
	with (allow_page_locks = off)
go

create unique nonclustered index [NCIX_LockOwnersTable_Id]
	on [CoreWf.DurableInstancing].[LockOwnersTable] ([Id])
	with (ignore_dup_key = on, allow_page_locks = off)

create nonclustered index [NCIX_LockOwnersTable_LockExpiration]
	on [CoreWf.DurableInstancing].[LockOwnersTable] ([LockExpiration]) include ([WorkflowHostType], [MachineName])
	with (allow_page_locks = off)
go

create nonclustered index [NCIX_LockOwnersTable_WorkflowHostType]
	on [CoreWf.DurableInstancing].[LockOwnersTable] ([WorkflowHostType])
	with (allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[InstanceMetadataChangesTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[InstanceMetadataChangesTable]
go

create table [CoreWf.DurableInstancing].[InstanceMetadataChangesTable]
(
	[SurrogateInstanceId] bigint not null,
	[ChangeTime] bigint identity,
	[EncodingOption] tinyint not null,
	[Change] varbinary(max) not null
)
go

create unique clustered index [CIX_InstanceMetadataChangesTable]
	on [CoreWf.DurableInstancing].[InstanceMetadataChangesTable] ([SurrogateInstanceId], [ChangeTime])
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[ServiceDeploymentsTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[ServiceDeploymentsTable]
go

create table [CoreWf.DurableInstancing].[ServiceDeploymentsTable]
(
	[Id] bigint identity not null,
	[ServiceDeploymentHash] uniqueidentifier not null,
	[SiteName] nvarchar(max) not null,
	[RelativeServicePath] nvarchar(max) not null,
	[RelativeApplicationPath] nvarchar(max) not null,
	[ServiceName] nvarchar(max) not null,
	[ServiceNamespace] nvarchar(max) not null,
)
go

create unique clustered index [CIX_ServiceDeploymentsTable]
	on [CoreWf.DurableInstancing].[ServiceDeploymentsTable] ([Id])
	with (allow_page_locks = off)
go

create unique nonclustered index [NCIX_ServiceDeploymentsTable_ServiceDeploymentHash]
	on [CoreWf.DurableInstancing].[ServiceDeploymentsTable] ([ServiceDeploymentHash])
	with (ignore_dup_key = on, allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[InstancePromotedPropertiesTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[InstancePromotedPropertiesTable]
go

create table [CoreWf.DurableInstancing].[InstancePromotedPropertiesTable]
(
	  [SurrogateInstanceId] bigint not null,
      [PromotionName] nvarchar(400) not null,
      [Value1] sql_variant null,
      [Value2] sql_variant null,
      [Value3] sql_variant null,
      [Value4] sql_variant null,
      [Value5] sql_variant null,
      [Value6] sql_variant null,
      [Value7] sql_variant null,
      [Value8] sql_variant null,
      [Value9] sql_variant null,
      [Value10] sql_variant null,
      [Value11] sql_variant null,
      [Value12] sql_variant null,
      [Value13] sql_variant null,
      [Value14] sql_variant null,
      [Value15] sql_variant null,
      [Value16] sql_variant null,
      [Value17] sql_variant null,
      [Value18] sql_variant null,
      [Value19] sql_variant null,
      [Value20] sql_variant null,
      [Value21] sql_variant null,
      [Value22] sql_variant null,
      [Value23] sql_variant null,
      [Value24] sql_variant null,
      [Value25] sql_variant null,
      [Value26] sql_variant null,
      [Value27] sql_variant null,
      [Value28] sql_variant null,
      [Value29] sql_variant null,
      [Value30] sql_variant null,
      [Value31] sql_variant null,
      [Value32] sql_variant null,
      [Value33] varbinary(max) null,
      [Value34] varbinary(max) null,
      [Value35] varbinary(max) null,
      [Value36] varbinary(max) null,
      [Value37] varbinary(max) null,
      [Value38] varbinary(max) null,
      [Value39] varbinary(max) null,
      [Value40] varbinary(max) null,
      [Value41] varbinary(max) null,
      [Value42] varbinary(max) null,
      [Value43] varbinary(max) null,
      [Value44] varbinary(max) null,
      [Value45] varbinary(max) null,
      [Value46] varbinary(max) null,
      [Value47] varbinary(max) null,
      [Value48] varbinary(max) null,
      [Value49] varbinary(max) null,
      [Value50] varbinary(max) null,
      [Value51] varbinary(max) null,
      [Value52] varbinary(max) null,
      [Value53] varbinary(max) null,
      [Value54] varbinary(max) null,
      [Value55] varbinary(max) null,
      [Value56] varbinary(max) null,
      [Value57] varbinary(max) null,
      [Value58] varbinary(max) null,
      [Value59] varbinary(max) null,
      [Value60] varbinary(max) null,
      [Value61] varbinary(max) null,
      [Value62] varbinary(max) null,
      [Value63] varbinary(max) null,
      [Value64] varbinary(max) null
)
go

create unique clustered index [CIX_InstancePromotedPropertiesTable]
	on [CoreWf.DurableInstancing].[InstancePromotedPropertiesTable] ([SurrogateInstanceId], [PromotionName])
	with (allow_page_locks = off)
go

if exists (select * from sys.objects where object_id = object_id(N'[CoreWf.DurableInstancing].[SqlWorkflowInstanceStoreVersionTable]') and type in (N'U'))
	drop table [CoreWf.DurableInstancing].[SqlWorkflowInstanceStoreVersionTable]
go

create table [CoreWf.DurableInstancing].[SqlWorkflowInstanceStoreVersionTable]
(
	[Major] bigint,
	[Minor] bigint,
	[Build] bigint,
	[Revision] bigint,
	[LastUpdated] datetime
)
go

create unique clustered index [CIX_SqlWorkflowInstanceStoreVersionTable]
	on [CoreWf.DurableInstancing].[SqlWorkflowInstanceStoreVersionTable] ([Major], [Minor], [Build], [Revision])
go

insert into [CoreWf.DurableInstancing].[SqlWorkflowInstanceStoreVersionTable]
values (4, 0, 0, 0, getutcdate())

if exists (select * from sys.views where object_id = object_id(N'[CoreWf.DurableInstancing].[Instances]'))
      drop view [CoreWf.DurableInstancing].[Instances]
go

create view [CoreWf.DurableInstancing].[Instances] as
      select [InstancesTable].[Id] as [InstanceId],
             [PendingTimer],
             [CreationTime],
             [LastUpdated] as [LastUpdatedTime],
             [InstancesTable].[ServiceDeploymentId],
             [SuspensionExceptionName],
             [SuspensionReason],
             [BlockingBookmarks] as [ActiveBookmarks],
             case when [LockOwnersTable].[LockExpiration] > getutcdate()
				then [LockOwnersTable].[MachineName]
				else null
				end as [CurrentMachine],
             [LastMachineRunOn] as [LastMachine],
             [ExecutionStatus],
             [IsInitialized],
             [IsSuspended],
             [IsCompleted],
             [InstancesTable].[DataEncodingOption] as [EncodingOption],
             [PrimitiveDataProperties] as [ReadWritePrimitiveDataProperties],
             [WriteOnlyPrimitiveDataProperties],
             [ComplexDataProperties] as [ReadWriteComplexDataProperties],
             [WriteOnlyComplexDataProperties]
      from [CoreWf.DurableInstancing].[InstancesTable]
      left outer join [CoreWf.DurableInstancing].[LockOwnersTable]
      on [InstancesTable].[SurrogateLockOwnerId] = [LockOwnersTable].[SurrogateLockOwnerId]
go

grant select on object::[CoreWf.DurableInstancing].[Instances] to [CoreWf.DurableInstancing.InstanceStoreObservers]
go

grant delete on object::[CoreWf.DurableInstancing].[Instances] to [CoreWf.DurableInstancing.InstanceStoreUsers]
go

if exists (select * from sys.views where object_id = object_id(N'[CoreWf.DurableInstancing].[ServiceDeployments]'))
      drop view [CoreWf.DurableInstancing].[ServiceDeployments]
go

create view [CoreWf.DurableInstancing].[ServiceDeployments] as
      select [Id] as [ServiceDeploymentId],
             [SiteName],
             [RelativeServicePath],
             [RelativeApplicationPath],
             [ServiceName],
             [ServiceNamespace]
      from [CoreWf.DurableInstancing].[ServiceDeploymentsTable]
go

grant select on object::[CoreWf.DurableInstancing].[ServiceDeployments] to [CoreWf.DurableInstancing.InstanceStoreObservers]
go

grant delete on object::[CoreWf.DurableInstancing].[ServiceDeployments] to [CoreWf.DurableInstancing.InstanceStoreUsers]
go

create view [CoreWf.DurableInstancing].[InstancePromotedProperties] with schemabinding as
      select [InstancesTable].[Id] as [InstanceId],
			 [InstancesTable].[DataEncodingOption] as [EncodingOption],
			 [PromotionName],
			 [Value1],
			 [Value2],
			 [Value3],
			 [Value4],
			 [Value5],
			 [Value6],
			 [Value7],
			 [Value8],
			 [Value9],
			 [Value10],
			 [Value11],
			 [Value12],
			 [Value13],
			 [Value14],
			 [Value15],
			 [Value16],
			 [Value17],
			 [Value18],
			 [Value19],
			 [Value20],
			 [Value21],
			 [Value22],
			 [Value23],
			 [Value24],
			 [Value25],
			 [Value26],
			 [Value27],
			 [Value28],
			 [Value29],
			 [Value30],
			 [Value31],
			 [Value32],
			 [Value33],
			 [Value34],
			 [Value35],
			 [Value36],
			 [Value37],
			 [Value38],
			 [Value39],
			 [Value40],
			 [Value41],
			 [Value42],
			 [Value43],
			 [Value44],
			 [Value45],
			 [Value46],
			 [Value47],
			 [Value48],
			 [Value49],
			 [Value50],
			 [Value51],
			 [Value52],
			 [Value53],
			 [Value54],
			 [Value55],
			 [Value56],
			 [Value57],
			 [Value58],
			 [Value59],
			 [Value60],
			 [Value61],
			 [Value62],
			 [Value63],
			 [Value64]
    from [CoreWf.DurableInstancing].[InstancePromotedPropertiesTable]
    join [CoreWf.DurableInstancing].[InstancesTable]
    on [InstancePromotedPropertiesTable].[SurrogateInstanceId] = [InstancesTable].[SurrogateInstanceId]
go

grant select on object::[CoreWf.DurableInstancing].[InstancePromotedProperties] to [CoreWf.DurableInstancing.InstanceStoreObservers]
go
