﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public static class TypeConverters
{
    public const string ActivityWithResultConverter = "System.Activities.XamlIntegration.ActivityWithResultConverter, UiPath.Workflow";
    public const string InArgumentConverter = "System.Activities.XamlIntegration.InArgumentConverter, UiPath.Workflow";
    public const string OutArgumentConverter = "System.Activities.XamlIntegration.OutArgumentConverter, UiPath.Workflow";
    public const string InOutArgumentConverter = "System.Activities.XamlIntegration.InOutArgumentConverter, UiPath.Workflow";
    public const string ImplementationVersionConverter = "System.Activities.XamlIntegration.ImplementationVersionConverter, UiPath.Workflow";
    public const string AssemblyReferenceConverter = "System.Activities.XamlIntegration.AssemblyReferenceConverter, UiPath.Workflow";
    public const string WorkflowIdentityConverter = "System.Activities.XamlIntegration.WorkflowIdentityConverter, UiPath.Workflow";
}

public static class OtherXaml
{
    public const string FuncDeferringLoader = "System.Activities.XamlIntegration.FuncDeferringLoader, UiPath.Workflow";
    public const string Activity = "System.Activities.Activity, System.Activities";
    public const string ArgumentValueSerializer = "System.Activities.XamlIntegration.ArgumentValueSerializer, UiPath.Workflow";
}
