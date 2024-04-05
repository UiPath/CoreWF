// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

// Define XAML namespace mappings
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities.Statements")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities.Expressions")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities.Validation")]
[assembly:
    XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities.XamlIntegration")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "Microsoft.CSharp.Activities")]
[assembly:
    XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "Microsoft.VisualBasic.Activities")]
[assembly:
    XmlnsCompatibleWith("clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities",
        "http://schemas.microsoft.com/netfx/2009/xaml/activities")]
[assembly:
    XmlnsCompatibleWith("clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities",
        "http://schemas.microsoft.com/netfx/2009/xaml/activities")]

[assembly:
    XmlnsDefinition("http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger",
        "System.Activities.Debugger.Symbol")]
[assembly: XmlnsPrefix("http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger", "sads")]

[assembly: InternalsVisibleTo("UiPath.Executor.Core")]
