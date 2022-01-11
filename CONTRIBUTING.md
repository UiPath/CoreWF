# Contributing to CoreWF
This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## Ways to contribute
While features and fixes are greatly appreciated, there are other ways to contribute to this project. If you find a bug, create a test that reveals the bug. If you find a performance issue, create an issue with a PerfView profile, memory dump, or even a [BenchmarkDotNet](https://benchmarkdotnet.org/) benchmark.

## Developing with .NET Core SDK
CoreWF requires .NET 6 or higher. Download and install from [here](https://dotnet.microsoft.com/download).

From the command prompt, navigate to the **src** folder. To build, run the command:
```
dotnet build
```

To test, run the command:
```
dotnet test
```

## Developing with Visual Studio 2022
Any edition of Visual Studio 2022 will work for developing CoreWF. The community edition is available free from [here](https://visualstudio.microsoft.com/vs/community/). Make sure that the **.NET Core cross-platform development** workload is selected. 

Open the **UiPath.Workflow.sln** file under the **src** folder. The Test Explorer will locate all the tests in the solution automatically.
