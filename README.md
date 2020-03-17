[![Build status](https://uipath.visualstudio.com/Core%20WF/_apis/build/status/CI)](https://uipath.visualstudio.com/Core%20WF/_build/latest?definitionId=318)
[![MyGet (dev)](https://img.shields.io/badge/CoreWf-MyGet-brightgreen.svg)](https://www.myget.org/feed/uipath-dev/package/nuget/UiPath.Workflow)
# Core WF
A port of the Windows Workflow Foundation (WF) runtime to .NET Core.

__This is not an official Microsoft release of WF on .NET Core. Core WF is a derivative work of Microsoft's copyrighted Windows Workflow Foundation.__

To add this library to your project, use the [NuGet package](https://www.myget.org/feed/uipath-dev/package/nuget/UiPath.Workflow).
ETW tracking provider is in a separate package [here](https://www.nuget.org/packages/CoreWf.EtwTracking/).

## A call for help from the community

The Windows Workflow Foundation (WF) handles the long-running work of many companies. It 
powers SharePoint workflows, PowerShell workflows, Team Foundation Server build 
processes, and many applications in all types of businesses. As more developers look into
adopting .NET Core, some are asking if WF will be officially ported. This project only 
ports the WF runtime and the ETW tracking provider.
