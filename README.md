# Core WF
The Windows Workflow Foundation (WF) runtime ported to work on .NET Core

__This is not an official Microsoft release of WF on .NET Core.__

The team that owns WF at Microsoft experimented with porting WF to .NET Core. However, the project did not gain much traction and will not be released officially. There are also several components not available in .NET Core 1.0, and not planned for future releases, that would be required for feature parity with the .NET Framework version. This project only ports the WF runtime and ETW tracking provider.

## Features Removed
Due to limitations with .NET Core, several features have been cut from the .NET Framework version of WF.

|Feature|Reason|
|:--------|:----------|
|Dynamic Update|Depends heavily on S.ComponentModel classes that have not been ported to Core|
|XAML Integration|S.Xaml has not been ported to Core and there are no plans to do so|
|Debugger Integration|Depends on XAML integration|
|Automatic CacheMetadata|In .NET Framework WF, if you write a custom activity you do not have to implement CacheMetadata. The WF runtime will use S.ComponentModel and reflection to determine your arguments and other properties. This is not supported on Core|
|Transactions Support|S.Transactions has not *yet* been ported to Core|
|C#/VB Expressions|Parsers not available on Core. I'm hoping to incorporate Roslyn for this purpose.|

## Feature Differences

|Feature|Difference|
|:--------|:----------|
|Persistence|Workflow instances are persisted using the [NetDataContractSerializer](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.netdatacontractserializer.aspx). The advantage of NetDCS is that it writes type information into the serialized XML. NetDCS has not been ported to .Net Core. Newtonsoft's JSON serializer is a suitable replacement.|
|XAML|Serializing workflow definitions does not have to be done in XAML. Testing is currently done with workflows created in code, but I'm looking at trying new formats for workflow definitions.|
|ETW tracing|This has been replaced with [EventSource](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.aspx) which can work on other platforms besides Windows|
|ETW tracking participant|Also replaced with EventSource.|

---
