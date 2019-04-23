[![Build status](https://uipath.visualstudio.com/Core%20WF/_apis/build/status/CI)](https://uipath.visualstudio.com/Core%20WF/_build/latest?definitionId=318)
[![MyGet (dev)](https://img.shields.io/badge/CoreWf-MyGet-brightgreen.svg)](https://www.myget.org/feed/uipath-dev/package/nuget/System.Activities)
# Core WF
A port of the Windows Workflow Foundation (WF) runtime to the .NET Standard.

__This is not an official Microsoft release of WF on .NET Core. Core WF is a derivative work of Microsoft's copyrighted Windows Workflow Foundation.__

To add this library to your project, use the [NuGet package](https://www.myget.org/feed/uipath-dev/package/nuget/System.Activities).
ETW tracking provider is in a separate package [here](https://www.nuget.org/packages/CoreWf.EtwTracking/).

## A call for help from the community

The Windows Workflow Foundation (WF) handles the long-running work of many companies. It 
powers SharePoint workflows, PowerShell workflows, Team Foundation Server build 
processes, and many applications in all types of businesses. As more developers look into
adopting .NET Core, some are asking if WF will be officially ported. This project only 
ports the WF runtime and ETW tracking provider to the .NET Standard. But much more work 
is needed before it can substitute for the .NET Framework version. 

The problem with porting is that WF integrates heavily with other features of the .NET 
Framework that are not being ported to .NET Core. The most sizable features are:

* XAML - replaced with Portable.Xaml

None of these components are trivial. I'll do my best to describe the options for each 
one.

### Instance stores 
The .NET Framework shipped with the SQL Workflow Instance Store (SWIS). This should be a straightforward port to 
the .NET Standard ([issue link](https://github.com/dmetzgar/corewf/issues/15)).

It is possible to implement your own instance store by implementing the abstract 
[InstanceStore](https://msdn.microsoft.com/en-us/library/system.runtime.durableinstancing.instancestore(v=vs.110).aspx) 
class. There are other implementations out there and it would be great to port them.
