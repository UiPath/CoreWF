// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

// Define XAML namespace mappings
[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/activities", "System.Activities.XamlIntegration")]
[assembly: InternalsVisibleTo("UiPath.Workflow")]
#if NET45

[assembly: XmlnsDefinition("http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger", "System.Activities.Debugger.Symbol")]
[assembly: XmlnsPrefix("http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger", "sads")]

#endif
