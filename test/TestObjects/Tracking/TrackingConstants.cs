// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Test.Common.TestObjects.Tracking
{
    // enums for Tracking Tests
    public enum TrackingParticipantType
    {
        //Do not change these names as the tests namespaces are dependant on these names
        InMemoryTrackingParticipant,
        SqlTrackingParticipant,
        CustomTrackingParticipant1,
        CustomTrackingParticipant2,
        CustomTrackingParticipant1WithException,
        CustomTrackingParticipant2WithException,
        CustomTrackingParticipantWithDelay,
        ETWTrackingParticipant,
        CustomTrackingRecordParticipant,
        CustomTrackingParticipantThrowFaultException,
        CustomTrackingParticipantThrowNonFaultException,
        CustomTrackingRecordParticipantWithDelay,
        CustomTrackingRecordParticipantWithClean,
        StateMachineTrackingParticipant
    }


    public enum TestProfileType
    {
        //General Profiles
        AllTrackpointsProfile,
        AllWfTrackpointsProfile,
        AllActivityTrackpointsProfile,
        NoProfile,
        DefaultITMonitoringProfile,
        CustomWFEventsActivityCompletedOnly,
        CustomWFEventsActivityExecutingOnly,
        EmptyProfile,
        RandomProfile,
        UnavailableProfile,
        NullProfile,
        AllUserTrackRecordsProfile,
        CustomTrackRecordsEmptyNameProfile,
        CustomTrackRecordsInvalidNameProfile,
        FaultPropagationProfile,
        ProfileScopeTarget,

        //Only one type of trackpoint
        WFInstanceTrackpointsOnlyProfile,
        ActivityTrackpointsOnlyProfile,
        BookmarkTrackpointsOnlyProfile,
        ActivityandBookmarkOnlyProfile,

        //2 types of trackpoint only
        WFInstanceandActivityOnlyProfile,
        WFInstanceandBookmarkOnlyProfile,

        //Filtering profiles
        ActivityTrackpointOnlyAllActivities1State,
        ActivityTrackpointOnlyAllActivities2States,
        ActivityTrackpointOnlyAllActivitiesAllStates,
        WFInstanceTrackpointOnlyAllActivities1State,
        WFInstanceTrackpointOnlyAllActivities2State,
        WFInstanceTrackpointOnlyAllActivitiesAllState,
        ActivityTrackpointOnly1Activity1State,
        ActivityTrackpointOnly2Activity2State,
        BookmarkTrackpointAllActivities1State,
        ActivityandBookmarkTrackpointSomeActivitiesSomeStates,
        WFInstanceandBookmarkTrackpointSomeActivitiesSomeStates,
        AllTrackpointsSomeActivitiesAllStates,
        WorkflowAllStateNamesIncorrect,
        WorkflowAllActivityNamesIncorrect,
        WFSomeStateNamesIncorrect,
        WorkflowSomeActivityNamesIncorrect,
        WorkflowCombinationofActivityAndStateNamesIncorrect,
        StateMachineTrackpointsOnly,

        //Data Items Extraction Profiles
        CustomProfile1VariableSameType1Event,
        CustomProfile2VariablesSameType1Event,
        CustomProfileAllVariablesSameType1Event,
        CustomProfile1VariableSameType3Events,
        CustomProfile2VariablesSameType3Events,
        CustomProfileAllVariablesSameType3Events,
        CustomProfile1EnvironmentPropertySameType1Event,
        CustomProfile2EnvironmentPropertySameType1Event,
        CustomProfileAllEnvironmentPropertySameType1Event,
        CustomProfile1EnvironmentPropertySameType3Events,
        CustomProfile2EnvironmentPropertySameType3Events,
        CustomProfileAllEnvironmentPropertySameType3Events,
        CustomProfile1Tag1Event,
        CustomProfile2Tag1Event,
        CustomProfile5Tag1Event,
        CustomProfile1Tag3Events,
        CustomProfile2Tags3Events,
        CustomProfile5Tags3Events,
        CustomProfile2VariablesDifferentTypes1Event,
        CustomProfileAllVariablesDifferentTypes1Event,
        CustomProfile2VariablesDifferentTypes3Events,
        CustomProfileAllVariablesDifferentTypes3Events,
        CustomProfile2EnvironmentPropertiesDifferentTypes1Event,
        CustomProfileAllEnvironmentPropertiesDifferentTypes1Event,
        CustomProfile2EnvironmentPropertiesDifferentTypes3Events,
        CustomProfileAllEnvironmentPropertiesDifferentTypes3Events,
        CustomProfile2Variablesand1EnvironmentPropertiesDifferentTypes1Event,
        CustomProfile2Variablesand3EnvironmentProperties1TagDifferentTypes1Event,
        CustomProfile2Variablesand3EnvironmentProperties1TagDifferentTypes3Events,
        CustomProfileIncorrectDataForBothParticipantsSomeEvents,
        CustomProfileIncorrectDataOnlyForCustomParticipantSomeEvents,
        CustomProfileIncorrectDataOnlyForMonitoringStoreParticipantSomeEvents,
        CustomProfileIncorrectDataForBothParticipantsAllEvents,
        CustomProfileDuplicateOutputVariableNames,
        CustomProfilePrivateVariableExtraction,
        CustomProfilePrivateJapaneseDataExtractionandTagNames,
        CustomProfilePrivateChineseDataExtractionandTagNames,
        CustomProfilePrivateGermanDataExtractionandTagNames,

        //Security Profiles
        FuzzedProfileStatus,
        FuzzedProfileStructure,
        SQLInjectionProfile,
        MissingActivityNameProfile,
        RandomFuzzedProfile欱欲欳欴欵欶欷欸欹欺欻欼欽款欿歀歁歂,

        // implementationVisibility Profile
        AllTrackpointsProfileRootScope,
        AllTrackpointsProfileRootScopeActivityNameSpecified
    }


    public static class WorkflowElementStates
    {
        public const string Started = "Started";
        public const string Idle = "Idle";
        public const string Closed = "Closed";
        public const string Resumed = "Resumed";
        public const string Completed = "Completed";
        public const string Executing = "Executing";
        public const string Faulted = "Faulted";
        public const string All = "*";
    }

    public static class ActivityStates
    {
        public const string Schedule = "Schedule";
        public const string Fault = "Fault";
        public const string Canceled = "Canceled";
        public const string Executing = "Executing";
    }

    public enum ActivityEventStatus
    {
        Executing,
        Schedule,
        Closed,
        Cancel,
        Fault,
    }

    public enum WorkflowEventStatus
    {
        Started,
        Idle,
        Resumed,
        Completed,
        Persisted,
        Unloaded,
        Deleted,
        UnhandledException,
    }


    public enum ProfileNameConfig
    {
        //ProfileOM Tests
        JustProfileName,
        ProfileNameAndScope,
        ProfileNameScopeAndScopeType,
        InvalidProfileName,
        InvalidProfileScope,
        InvalidProfileScopeType,
        ExtremelyLongProfileName,
        ExtremelyLongProfileNameInvalid,
        EmptyProfileName,
        //GlobLocProfiles
        ArabicProfile,
        JapaneseProfile,
        SpanishProfile,
        ChineseProfile,
    }

    public enum TransactionalConfig
    {
        //TransactionTests
        NotTransactional,
        Transactional,
    }


    public enum ParticipantConnStrConfig
    {
        //TransactionTests
        ValidConnectionString,
        InvalidConnectionString,
        StoreNotSetup,
        SQLServiceUnavailable,
        ReadRole,
        AdminRole,
        LocalizedDB
    }

    public enum RetriesTimeoutConfig : int
    {
        //TransactionTests
        NoTimeout = 0,
        DefaultTimeout = 10,
        MaxTimeout = 300,
        InvalidTimeout1 = 301,
        InvalidTimeout2 = -1,
    }

    public enum RunType
    {
        RunOnce,
        RunTwice,
        RunDehydRehyd
    }


    public enum ProfileManagerType
    {
        CodeProfileManager,
        ConfigProfileManager,
        CustomProfileManager1,
        CustomProfileManager2,
        ConfigProfileManagerwException,
        EmptyProfileManager,
        NoProfileReturnedPM
    }


    public enum ParticipantAssociation
    {
        WorkflowExtention,
        TestVerification
    }

    public enum EtwKeywords
    {
        HealthMonitoring,
        Diagnostics,
        WFTracking,
    }

    public enum TraceType
    {
        WorkflowApplication,
        Activity,
        Bookmark,
        User
    }

    public enum EtwEventId
    {
        WorkflowInstanceRecord = 100,
        WorkflowInstanceUnhandledExceptionRecord = 101,
        WorkflowInstanceAbortedRecord = 102,
        ActivityStateRecord = 103,
        ActivityScheduledRecord = 104,
        FaultPropagationRecord = 105,
        CancelRequestedRecord = 106,
        BookmarkResumptionRecord = 107,
        CustomTrackingRecordInfo = 108,
        ActivityInitializedRecord = 109,
        CustomTrackingRecordWarning = 110,
        CustomTrackingRecordError = 111,
        WorkflowInstanceSuspendedRecord = 112,
        WorkflowInstanceTerminatedRecord = 113,
        WorkflowInstanceRecordWithId = 114,
        WorkflowInstanceAbortedRecordWithId = 115,
        WorkflowInstanceSuspendedRecordWithId = 116,
        WorkflowInstanceTerminatedRecordWithId = 117,
        WorkflowInstanceUnhandledExceptionRecordWithId = 118,
        WorkflowInstanceUpdateRecord = 119,
        ExecuteWorkItemStart = 2021,
        ExecuteWorkItemStop = 2022,
        InternalCacheMetadataStart = 2024,
        InternalCacheMetadataStop = 2025,
        CompileVbExpression = 2026,
        CacheRootMetadataStart = 2027,
        CacheRootMetadataStop = 2028,
    }

    public enum DebugEventId
    {
        ThrowingException = 57396
    }

    public static class LogManKeywords
    {
        public const string WFTracking = "0x40";
        public const string Debug = "0x1000000000000000";
        public const string AllTraces = "0xffffffffffffffff";
    }


    public static class TrackingConstants
    {
        public const string ExceptionMessage = "This is TrackingParticipantWithException throwing an exception on Track()";
        public const string DefaultProviderId = "{c651f5f6-1c0d-492e-8ae1-b4efd7c9d503}";
        public const string TrackingETWTestSessionName = "TrackingTestSession";
        public readonly static string RemoteInMemoryTrackingParticipantNs = typeof(Test.Common.TestObjects.Tracking.RemoteInMemoryTrackingParticipant).AssemblyQualifiedName;
    }
}
