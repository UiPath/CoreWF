// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Debugger.Symbol;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xaml;

namespace System.Activities.Debugger;

// Provide SourceLocation information for activities in given root activity.
// This is integration point with Workflow project system (TBD).
// The current plan is to get SourceLocation from (in this order):
//  1. pdb (when available)
//  2a. parse xaml files available in the same project (or metadata store) or
//  2b. ask user to point to the correct xaml source.
//  3.  Publish (serialize to tmp file) and deserialize it to collect SourceLocation (for loose xaml).
// Current code cover only step 3.

[DebuggerNonUserCode]
public static class SourceLocationProvider
{
    [Fx.Tag.Throws(typeof(Exception), "Calls Serialize/Deserialize to temporary file")]
    internal static Dictionary<object, SourceLocation> GetSourceLocations(Activity rootActivity, out string sourcePath,
        out bool isTemporaryFile, out byte[] checksum)
    {
        isTemporaryFile = false;
        checksum = null;
        var symbolString = DebugSymbol.GetSymbol(rootActivity) as string;
        if (string.IsNullOrEmpty(symbolString) && rootActivity.Children != null && rootActivity.Children.Count > 0)
        {
            // In case of actual root is wrapped either in x:Class activity or CorrelationScope
            var body = rootActivity.Children[0];
            var bodySymbolString = DebugSymbol.GetSymbol(body) as string;
            if (!string.IsNullOrEmpty(bodySymbolString))
            {
                rootActivity = body;
                symbolString = bodySymbolString;
            }
        }

        if (!string.IsNullOrEmpty(symbolString))
        {
            try
            {
                var wfSymbol = WorkflowSymbol.Decode(symbolString);
                if (wfSymbol != null)
                {
                    sourcePath = wfSymbol.FileName;
                    checksum = wfSymbol.GetChecksum();
                    // rootActivity is the activity with the attached symbol string.
                    // rootActivity.RootActivity is the workflow root activity.
                    // if they are not the same, then it must be compiled XAML, because loose XAML (i.e. XAMLX) always have the symbol attached at the root.
                    if (rootActivity.RootActivity != rootActivity)
                    {
                        Fx.Assert(rootActivity.Parent != null, "Compiled XAML implementation always have a parent.");
                        rootActivity = rootActivity.Parent;
                    }

                    return GetSourceLocations(rootActivity, wfSymbol, false);
                }
            }
            catch (SerializationException)
            {
                // Ignore invalid symbol.
            }
        }

        sourcePath = XamlDebuggerXmlReader.GetFileName(rootActivity) as string;
        Dictionary<object, SourceLocation> mapping;
        Assembly localAssembly;
        //bool permissionRevertNeeded = false;

        // This may not be the local assembly since it may not be the real root for x:Class 
        localAssembly = rootActivity!.GetType().Assembly;

        if (rootActivity.Parent != null)
        {
            localAssembly = rootActivity.Parent.GetType().Assembly;
        }

        if (rootActivity.Children is {Count: > 0})
        {
            // In case of actual root is wrapped either in x:Class activity or CorrelationScope
            var body = rootActivity.Children[0];
            var bodySourcePath = XamlDebuggerXmlReader.GetFileName(body) as string;
            if (!string.IsNullOrEmpty(bodySourcePath))
            {
                rootActivity = body;
                sourcePath = bodySourcePath;
            }
        }

        Fx.Assert(!string.IsNullOrEmpty(sourcePath),
            "If sourcePath is null, it should have been short-circuited before reaching here.");

        Activity tempRootActivity;

        checksum = SymbolHelper.CalculateChecksum(sourcePath);
        if (TryGetSourceLocation(rootActivity, sourcePath, checksum, out _)) // already has source location.
        {
            tempRootActivity = rootActivity;
        }
        else
        {
            var fi = new FileInfo(sourcePath!);
            var buffer = new byte[fi.Length];

            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);
            }

            var deserializedObject = Deserialize(buffer, localAssembly);
            if (deserializedObject is IDebuggableWorkflowTree debuggableWorkflowTree)
                // Declarative Service and x:Class case
            {
                tempRootActivity = debuggableWorkflowTree.GetWorkflowRoot();
            }
            else
                // Loose XAML case.
            {
                tempRootActivity = deserializedObject as Activity;
            }

            Fx.Assert(tempRootActivity != null, "Unexpected workflow xaml file");
        }

        mapping = new Dictionary<object, SourceLocation>();
        if (tempRootActivity != null)
        {
            CollectMapping(rootActivity, tempRootActivity, mapping, sourcePath, checksum);
        }

        return mapping;
    }

    public static Dictionary<object, SourceLocation> GetSourceLocations(Activity rootActivity, WorkflowSymbol symbol) =>
        GetSourceLocations(rootActivity, symbol, true);

    // For most of the time, we need source location for object that appear on XAML.
    // During debugging, however, we must not transform the internal activity to their origin to make sure it stop when the internal activity is about the execute
    // Therefore, in debugger scenario, translateInternalActivityToOrigin will be set to false.
    internal static Dictionary<object, SourceLocation> GetSourceLocations(Activity rootActivity, WorkflowSymbol symbol,
        bool translateInternalActivityToOrigin)
    {
        var workflowRoot = rootActivity.RootActivity ?? rootActivity;
        if (!workflowRoot.IsMetadataFullyCached)
        {
            IList<ValidationError> validationErrors = null;
            ActivityUtilities.CacheRootMetadata(workflowRoot, new ActivityLocationReferenceEnvironment(),
                ProcessActivityTreeOptions.ValidationOptions, null, ref validationErrors);
        }

        var newMapping = new Dictionary<object, SourceLocation>();

        // Make sure the qid we are using to TryGetElementFromRoot
        // are shifted appropriately such that the first digit that QID is
        // the same as the last digit of the rootActivity.QualifiedId.

        var rootIdArray = rootActivity.QualifiedId.AsIDArray();
        var idOffset = rootIdArray[^1] - 1;

        foreach (var actSym in symbol.Symbols)
        {
            var qid = new QualifiedId(actSym.QualifiedId);
            if (idOffset != 0)
            {
                var idArray = qid.AsIDArray();
                idArray[0] += idOffset;
                qid = new QualifiedId(idArray);
            }

            if (QualifiedId.TryGetElementFromRoot(rootActivity, qid, out var activity))
            {
                object origin = activity;
                if (translateInternalActivityToOrigin && activity.Origin != null)
                {
                    origin = activity.Origin;
                }

                newMapping.Add(origin,
                    new SourceLocation(symbol.FileName, symbol.GetChecksum(), actSym.StartLine, actSym.StartColumn,
                        actSym.EndLine, actSym.EndColumn));
            }
        }

        return newMapping;
    }

    [Fx.Tag.SecurityNote(Miscellaneous =
        "RequiresReview - We are deserializing XAML from a file. The file may have been read under and Assert for FileIOPermission. The data should be validated and not cached.")]
    internal static object Deserialize(byte[] buffer, Assembly localAssembly)
    {
        using var memoryStream = new MemoryStream(buffer);
        using TextReader streamReader = new StreamReader(memoryStream);
        using var xamlDebuggerReader =
            new XamlDebuggerXmlReader(streamReader, new XamlSchemaContext(), localAssembly);
        xamlDebuggerReader.SourceLocationFound += XamlDebuggerXmlReader.SetSourceLocation;

        using var activityBuilderReader = ActivityXamlServices.CreateBuilderReader(xamlDebuggerReader);
        return XamlServices.Load(activityBuilderReader);
    }

    public static void CollectMapping(Activity rootActivity1, Activity rootActivity2,
        Dictionary<object, SourceLocation> mapping, string path)
    {
        CollectMapping(rootActivity1, rootActivity2, mapping, path, null, true);
    }

    // Collect mapping for activity1 and its descendants to their corresponding source location.
    // activity2 is the shadow of activity1 but with SourceLocation information.
    [Fx.Tag.SecurityNote(Miscellaneous =
        "RequiresReview - We are dealing with activity and SourceLocation information that came from the user, possibly under an Assert for FileIOPermission. The data should be validated and not cached.")]
    private static void CollectMapping(Activity rootActivity1, Activity rootActivity2,
        IDictionary<object, SourceLocation> mapping, string path, byte[] checksum, bool requirePrepareForRuntime)
    {
        // For x:Class, the rootActivity here may not be the real root, but it's the first child of the x:Class activity.
        var realRoot1 = rootActivity1.RootActivity ?? rootActivity1;
        if (requirePrepareForRuntime && !realRoot1.IsRuntimeReady ||
            !requirePrepareForRuntime && !realRoot1.IsMetadataFullyCached)
        {
            IList<ValidationError> validationErrors = null;
            ActivityUtilities.CacheRootMetadata(realRoot1, new ActivityLocationReferenceEnvironment(),
                ProcessActivityTreeOptions.ValidationOptions, null, ref validationErrors);
        }

        // Similarly for rootActivity2.
        var realRoot2 = rootActivity2.RootActivity ?? rootActivity2;
        if (rootActivity1 != rootActivity2 && requirePrepareForRuntime && !realRoot2.IsRuntimeReady ||
            !requirePrepareForRuntime && !realRoot2.IsMetadataFullyCached)
        {
            IList<ValidationError> validationErrors = null;
            ActivityUtilities.CacheRootMetadata(realRoot2, new ActivityLocationReferenceEnvironment(),
                ProcessActivityTreeOptions.ValidationOptions, null, ref validationErrors);
        }

        var pairsRemaining = new Queue<KeyValuePair<Activity, Activity>>();

        pairsRemaining.Enqueue(new KeyValuePair<Activity, Activity>(rootActivity1, rootActivity2));
        KeyValuePair<Activity, Activity> currentPair;
        var visited = new HashSet<Activity>();

        while (pairsRemaining.Count > 0)
        {
            currentPair = pairsRemaining.Dequeue();
            var activity1 = currentPair.Key;
            var activity2 = currentPair.Value;

            visited.Add(activity1);

            if (TryGetSourceLocation(activity2, path, checksum, out var sourceLocation))
            {
                mapping.Add(activity1, sourceLocation);
            }
            else if (!(activity2 is IExpressionContainer ||
                     activity2 is IValueSerializableExpression)) // Expression is known not to have source location.
                //Some activities may not have corresponding Xaml node, e.g. ActivityFaultedOutput.                    
            {
                Trace.WriteLine("WorkflowDebugger: Does not have corresponding Xaml node for: " +
                    activity2.DisplayName + "\n");
            }

            // This to avoid comparing any value expression with DesignTimeValueExpression (in designer case).
            if (!(activity1 is IExpressionContainer || activity2 is IExpressionContainer ||
                activity1 is IValueSerializableExpression || activity2 is IValueSerializableExpression))
            {
                using var enumerator1 = WorkflowInspectionServices.GetActivities(activity1).GetEnumerator();
                using var enumerator2 = WorkflowInspectionServices.GetActivities(activity2).GetEnumerator();
                var hasNextItem1 = enumerator1.MoveNext();
                var hasNextItem2 = enumerator2.MoveNext();
                while (hasNextItem1 && hasNextItem2)
                {
                    if (!visited.Contains(enumerator1
                            .Current)) // avoid adding the same activity (e.g. some default implementation).
                    {
                        if (enumerator1.Current.GetType() != enumerator2.Current.GetType())
                            // Give debugger log instead of just asserting; to help user find out mismatch problem.
                        {
                            Trace.WriteLine(
                                "Unmatched type: " + enumerator1.Current.GetType().FullName +
                                " vs " + enumerator2.Current.GetType().FullName + "\n");
                        }

                        pairsRemaining.Enqueue(
                            new KeyValuePair<Activity, Activity>(enumerator1.Current, enumerator2.Current));
                    }

                    hasNextItem1 = enumerator1.MoveNext();
                    hasNextItem2 = enumerator2.MoveNext();
                }

                // If enumerators do not finish at the same time, then they have unmatched number of activities.
                // Give debugger log instead of just asserting; to help user find out mismatch problem.
                if (hasNextItem1 || hasNextItem2)
                {
                    Trace.WriteLine("Unmatched number of children\n");
                }
            }
        }
    }

    private static void CollectMapping(Activity rootActivity1, Activity rootActivity2,
        Dictionary<object, SourceLocation> mapping, string path, byte[] checksum)
    {
        CollectMapping(rootActivity1, rootActivity2, mapping, path, checksum, true);
    }

    // Get SourceLocation for object deserialized with XamlDebuggerXmlReader in deserializer stack.
    private static bool TryGetSourceLocation(object obj, string path, byte[] checksum,
        out SourceLocation sourceLocation)
    {
        sourceLocation = null;

        if (AttachablePropertyServices.TryGetProperty(obj, XamlDebuggerXmlReader.StartLineName, out int startLine) &&
            AttachablePropertyServices.TryGetProperty(obj, XamlDebuggerXmlReader.StartColumnName, out int startColumn) &&
            AttachablePropertyServices.TryGetProperty(obj, XamlDebuggerXmlReader.EndLineName, out int endLine) &&
            AttachablePropertyServices.TryGetProperty(obj, XamlDebuggerXmlReader.EndColumnName, out int endColumn) &&
            SourceLocation.IsValidRange(startLine, startColumn, endLine, endColumn))
        {
            sourceLocation = new SourceLocation(path, checksum, startLine, startColumn, endLine, endColumn);
            return true;
        }

        return false;
    }

    public static ICollection<ActivitySymbol> GetSymbols(Activity rootActivity,
        Dictionary<object, SourceLocation> sourceLocations)
    {
        var symbols = new List<ActivitySymbol>();
        var realRoot = rootActivity.RootActivity ?? rootActivity;
        if (!realRoot.IsMetadataFullyCached)
        {
            IList<ValidationError> validationErrors = null;
            ActivityUtilities.CacheRootMetadata(realRoot, new ActivityLocationReferenceEnvironment(),
                ProcessActivityTreeOptions.ValidationOptions, null, ref validationErrors);
        }

        var activitiesRemaining = new Queue<Activity>();
        activitiesRemaining.Enqueue(realRoot);
        var visited = new HashSet<Activity>();
        while (activitiesRemaining.Count > 0)
        {
            var currentActivity = activitiesRemaining.Dequeue();
            var origin = currentActivity.Origin ?? currentActivity;
            if (!visited.Contains(currentActivity) && sourceLocations.TryGetValue(origin, out var sourceLocation))
            {
                symbols.Add(new ActivitySymbol
                {
                    QualifiedId = currentActivity.QualifiedId.AsByteArray(),
                    StartLine = sourceLocation.StartLine,
                    StartColumn = sourceLocation.StartColumn,
                    EndLine = sourceLocation.EndLine,
                    EndColumn = sourceLocation.EndColumn
                });
            }

            visited.Add(currentActivity);
            foreach (var childActivity in WorkflowInspectionServices.GetActivities(currentActivity))
            {
                activitiesRemaining.Enqueue(childActivity);
            }
        }

        return symbols;
    }
}
