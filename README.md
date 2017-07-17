# Core WF
A port of the Windows Workflow Foundation (WF) runtime to the .NET Standard

__This is not an official Microsoft release of WF on .NET Core.__

To add this library to your project, use the [NuGet package](https://www.nuget.org/packages/CoreWf/).
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

* XAML
* VB/C# expression compilation
* WCF services
* WPF - for the WF designer

None of these components are trivial. I'll do my best to describe the options for each 
one.

### XAML
Here's a very simplified WF XAML file:

```xml
<Activity  
    x:Class="WorkflowConsoleApplication1.Workflow1" 
    xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Sequence />
</Activity>
```

All WF workflows are activities, meaning they inherit from the 
[`Activity`](http://referencesource.microsoft.com/#System.Activities/System/Activities/Activity.cs) 
base class. The workflow you design in the WF designer turns into a class. To make this happen, WF uses 
the [x:Class directive](https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-class-directive).

The magic all starts with a class called 
[`ActivityXamlServices`](http://referencesource.microsoft.com/#System.Activities/System/Activities/XamlIntegration/ActivityXamlServices.cs), 
which uses a custom [`XamlReader`](http://referencesource.microsoft.com/#System.Xaml/System/Xaml/XamlReader.cs) and 
[`XamlServices`](http://referencesource.microsoft.com/#System.Xaml/System/Xaml/XamlServices.cs) to turn the XAML into a 
[`DynamicActivity`](http://referencesource.microsoft.com/#System.Activities/System/Activities/DynamicActivity.cs) object. 

`DynamicActivity` was not part of our initial port of WF because of some missing parts in 
[System.ComponentModel](https://github.com/dotnet/corefx/tree/master/src/System.ComponentModel). More of this namespace 
has been filled out since .NET Core 1.0 so it is possible that there is enough there to implement `DynamicActivity`. 
Implementing `DynamicActivity` is the first foundational task that the community could help with 
([issue link](https://github.com/dmetzgar/corewf/issues/3)).

Once `DynamicActivity` is implemented, we'll need a XAML parser. We've spent some time experimenting with @cwensley's 
adaption of [Portable.Xaml](https://github.com/cwensley/Portable.Xaml) to the .NET Standard. It seems promising so far. 
The second task for the community is to use Portable.Xaml to implement the System.Activities.XamlIntegration
([issue link](https://github.com/dmetzgar/corewf/issues/6)).

### Expressions
A significant portion of XAML workflows use expressions. Expressions can be written in either C# or VB. The 
[`TextExpressionCompiler`](http://referencesource.microsoft.com/#System.Activities/System/Activities/XamlIntegration/TextExpressionCompiler.cs)
class is responsible for parsing expression strings into activities that the WF runtime can execute. 
`TextExpressionCompiler` will use the 
[Microsoft.VisualBasic](http://referencesource.microsoft.com/#Microsoft.VisualBasic,namespaces) 
libraries for VB expressions and the 
[XamlBuildTask](http://referencesource.microsoft.com/#XamlBuildTask)
MSBuild task for C# expressions. The VB compiler has both managed and native components and operates at build time. 
The C# compiler is basically just hooking in csc.exe during build time to generate a partial class. 

In the .NET Framework, it's not possible to take a dependency on the Roslyn libraries because those are external 
dependencies. A much better way to handle expressions is to use Roslyn to interpret the expressions and turn them 
into activities. This could be done either at runtime or build time (perhaps with a .NET SDK tool). The third task 
for the community is to write a Roslyn module that can turn an expression tree into an activity tree
([issue link](https://github.com/dmetzgar/corewf/issues/7)). It would also need to be integrated with the XAML parser.

### Workflow services
WF services can host workflows via web services. The built-in host is a subclass of 
[`ServiceHost`](http://referencesource.microsoft.com/#System.ServiceModel/System/ServiceModel/ServiceHost.cs)
called 
[`WorkflowServiceHost`](http://referencesource.microsoft.com/#System.ServiceModel.Activities/System/ServiceModel/Activities/WorkflowServiceHost.cs),
which is part of the System.ServiceModel.Activities library. `ServiceHost` hosts WCF services. In order to allow 
existing customers to port their applications over to .NET Core, we would need the WCF team to implement WCF 
server-side. Please chime in on [this issue](https://github.com/dotnet/wcf/issues/1200). 

Many WF users have asked for a REST implementation for WF. This was not really possible with the .NET Framework 
version since it would need to use Web API, which is not part of the .NET Framework. With .NET Core, we can take 
this dependency (preferably from a separate assembly). The tricky parts are how to handle sending and receiving 
messages. With the .NET Framework, you could generate a WCF client for a WF service. There would need to be a 
substitute for this that works with REST. Unfortunately, anyone porting an existing application that uses 
`WorkflowServiceHost` will also have to change their clients to talk to the REST WF service. An optional task 
then is to create a REST service host for WF ([issue link](https://github.com/dmetzgar/corewf/issues/8)).

### Workflow designer
The designer for WF workflows is not just a part of Visual Studio. The entire code is actually in the .NET 
Framework. This allows developers to "re-host" the WF designer in their own applications. A number of software 
vendors do this and have the WF designer ingrained into their product. Since there is no UI in .NET Core, besides 
web UI anyway, we won't be able to port the designer. 

There has been a long-standing request to build a HTML-based workflow designer and was attempted a few times. 
Some other products like [Node-RED](https://nodered.org/) have an excellent HTML designer already. One path is 
to make a brand new HTML designer for WF. Another is to try and take advantage of one that already exists. 

This is where another consideration comes into play. There is an industry standard for defining workflows called 
[BPML](https://en.wikipedia.org/wiki/Business_Process_Modeling_Language). If there was a parser that could 
convert BPML to activity objects or XAML, then we wouldn't need to create a new designer specifically for WF. We 
could instead use an existing designer that can produce BPML. That's assuming they're compatible, of course. 

This leaves us with two more possible tasks for the community: 
[create a HTML workflow designer using ASP.NET Core](https://github.com/dmetzgar/corewf/issues/9) or 
[create a BPML to WF converter](https://github.com/dmetzgar/corewf/issues/10).

### Instance stores 
The .NET Framework shipped with the SQL Workflow Instance Store (SWIS). This should be a straightforward port to 
the .NET Standard ([issue link](https://github.com/dmetzgar/corewf/issues/15)).

It is possible to implement your own instance store by implementing the abstract 
[InstanceStore](https://msdn.microsoft.com/en-us/library/system.runtime.durableinstancing.instancestore(v=vs.110).aspx) 
class. There are other implementations out there and it would be great to port them.
