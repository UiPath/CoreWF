// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Common.TestObjects.Utilities.Validation
{
    /// <summary>
    /// Validation engine
    /// </summary>
    public static class TraceValidator
    {
        private static ActualTrace s_actualTrace;
        private static ExpectedTrace s_expectedTrace;
        private static List<string> s_errorList;
        private static Dictionary<string, StepCount> s_stepCounts = new Dictionary<string, StepCount>();

        public static void Validate(ActualTrace actualTrace, ExpectedTrace expectedTrace)
        {
            Validate(actualTrace, expectedTrace, true);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public static void Validate(ActualTrace actualTrace, ExpectedTrace expectedTrace, bool traceTracking)
        {
            TraceValidator.s_actualTrace = actualTrace;
            TraceValidator.s_expectedTrace = expectedTrace;

            TraceValidator.s_errorList = new List<string>();
            TraceValidator.s_stepCounts = new Dictionary<string, StepCount>();

            TestTraceManager.OptionalLogTrace("[TraceValidator]Unfiltered expected trace:\n{0}", expectedTrace.ToString());
            TestTraceManager.OptionalLogTrace("[TraceValidator]Unfiltered actual trace:\n{0}", actualTrace.ToString());

            TraceValidator.NormalizeExpectedTrace(expectedTrace.Trace);
            TraceValidator.RemoveIgnorableSteps(expectedTrace.Trace);
            TraceValidator.PrepareExpectedTrace(expectedTrace.Trace, false, null, -1);

            TraceValidator.PrepareActualTrace();

            if (traceTracking)
            {
                //Log.TraceInternal("[TraceValidator]Filtered expected trace:\n{0}", expectedTrace.ToString());
                //Log.TraceInternal("[TraceValidator]Filtered actual trace:\n{0}", actualTrace.ToString());
                //Log.TraceInternal("[TraceValidator]Doing count validation...");
            }

            TraceValidator.CheckStepCounts();
            TraceValidator.CheckErrors();

            if (traceTracking)
            {
                //Log.TraceInternal("[TraceValidator]Validating...");
            }
            TraceValidator.ValidateFirst(expectedTrace.Trace, 0);
            TraceValidator.CheckErrors();

            if (traceTracking)
            {
                //Log.TraceInternal("[TraceValidator]ExpectedTrace: Validation complete.");
            }
        }

        private static void CheckErrors()
        {
            if (TraceValidator.s_errorList.Count > 0)
            {
                StringBuilder errors = new StringBuilder();

                foreach (string error in TraceValidator.s_errorList)
                {
                    errors.AppendLine(error);
                }

                throw new Exception(String.Format("Validation errors:\n{0}", errors.ToString()));
            }
        }

        #region Validation helper methods

        // <summary>
        // Prepares composite steps for validation
        // </summary>
        // <param name="stepCounter">StepCounter object to count optional and required steps</param>
        // <param name="parentIsOptional">Indicates if parent step is optional</param>
        // <param name="parentStep">parent step object</param>
        // <param name="indexInParent">index of this step inside its parent</param>
        private static void PrepareExpectedTrace(TraceGroup traceStep, bool parentIsOptional, TraceGroup parentStep, int indexInParent)
        {
            // set parent step reference for this step
            traceStep.parent = parentStep;
            traceStep.indexInParent = indexInParent;

            bool optional = parentIsOptional || traceStep.Optional;

            // set parent step reference for children steps
            for (int i = 0; i < traceStep.Steps.Count; i++)
            {
                WorkflowTraceStep childStep = traceStep.Steps[i];

                if (childStep is TraceGroup)
                {
                    TraceValidator.PrepareExpectedTrace((TraceGroup)childStep, optional, traceStep, i);
                }
                else if (childStep is IActualTraceStep)
                {
                    if (optional || childStep.Optional)
                    {
                        TraceValidator.AddExpectedOptStepCount((IActualTraceStep)childStep);
                    }
                    else
                    {
                        TraceValidator.AddExpectedReqStepCount((IActualTraceStep)childStep);
                    }
                }
            }

            // declare & init endIndexes[] array
            traceStep.endIndexes = new int[traceStep.Steps.Count];
            for (int i = 0; i < traceStep.Steps.Count; i++)
            {
                traceStep.endIndexes[i] = -1;
            }
        }


        // <summary>
        // Remove traces we can ignore
        // </summary>
        private static void RemoveIgnorableSteps(TraceGroup trace)
        {
            var ignorableSteps =
                (from step in trace.Steps
                 where step is IActualTraceStep &&
                 TraceValidator.s_expectedTrace.CanBeIgnored((IActualTraceStep)step)
                 select step).ToList();

            foreach (WorkflowTraceStep step in ignorableSteps)
            {
                trace.Steps.Remove(step);
            }

            foreach (WorkflowTraceStep step in trace.Steps)
            {
                if (step is TraceGroup)
                {
                    TraceValidator.RemoveIgnorableSteps((TraceGroup)step);
                }
            }
        }

        // <summary>
        // Prepare datastructure for validation
        // </summary>
        // <param name="expectedTrace">expected trace</param>
        private static void PrepareActualTrace()
        {
            // remove ignorable traces
            var ignorableSteps =
                (from step in TraceValidator.s_actualTrace.Steps
                 where TraceValidator.s_expectedTrace.CanBeIgnored(step)
                 select step).ToList();

            foreach (IActualTraceStep step in ignorableSteps)
            {
                TraceValidator.s_actualTrace.Steps.Remove(step);
            }

            foreach (IActualTraceStep step in TraceValidator.s_actualTrace.Steps)
            {
                step.Validated = 0;
                TraceValidator.AddActualStepCount(step);
            }
        }

        // <summary>
        // Find all non-validated occurences of the step
        // </summary>
        // <param name="stepLookingFor">step to look for</param>
        // <param name="startIndex">index to start search from</param>
        // <param name="mustBeAfter">look only for steps occured after time specified</param>
        // <returns>array of occurence indexes</returns>
        private static int[] FindAllEntries(IActualTraceStep stepLookingFor, int startIndex, DateTime mustBeAfter)
        {
            List<int> foundIndexes = new List<int>();

            for (int curIndex = startIndex; curIndex < TraceValidator.s_actualTrace.Steps.Count; curIndex++)
            {
                IActualTraceStep step = TraceValidator.s_actualTrace.Steps[curIndex];

                // Main step comparison
                if (step.Equals(stepLookingFor) &&
                    step.Validated == 0)
                {
                    // Remove this arbitrary 250 ms buffer to allow
                    // for the fact that this is a multithreaded system and
                    // therefore our "lastTime" isn't the actual time that the
                    // event occurred, but sometime after.
                    if (step.TimeStamp.AddMilliseconds(250) > mustBeAfter)
                    {
                        foundIndexes.Add(curIndex);
                    }
                    else
                    {
                        TraceValidator.s_errorList.Add(String.Format(
                            "Warning: step {0}, index {1} occured at {2}, but was expected after {3}",
                            stepLookingFor,
                            curIndex,
                            step.TimeStamp,
                            mustBeAfter));
                    }
                }
            }

            return foundIndexes.ToArray();
        }

        // <summary>
        // Mark step as verified
        // </summary>
        private static void MarkAsFound(int index, out DateTime timeStamp)
        {
            IActualTraceStep step = TraceValidator.s_actualTrace.Steps[index];

            if (step.Validated > 0)
            {
                throw new Exception("Internal validation failure: Attempt to mark as validated the step which already has this mark");
            }

            step.Validated = 1;
            timeStamp = step.TimeStamp;
        }

        // <summary>
        // Set restore point the validation can be rolled back to
        // Preserves the state of validated field of all actual trace steps
        // </summary>
        private static void SetRestorePoint()
        {
            foreach (IActualTraceStep step in TraceValidator.s_actualTrace.Steps)
            {
                if (step.Validated > 0)
                {
                    step.Validated++;
                }
            }
        }

        // <summary>
        // Roll back to the last restore point
        // Preserves the state of validated field of all actual trace steps
        // </summary>
        private static void Rollback()
        {
            foreach (IActualTraceStep step in TraceValidator.s_actualTrace.Steps)
            {
                if (step.Validated > 0)
                {
                    step.Validated--;
                }
            }
        }

        /// <summary>
        /// Commit the last point
        /// Preserves the state of validated field of all actual trace steps
        /// </summary>
        private static void Commit()
        {
            foreach (IActualTraceStep step in TraceValidator.s_actualTrace.Steps)
            {
                if (step.Validated > 1)
                {
                    step.Validated--;
                }
            }
        }

        #endregion // Validation helper methods

        #region Methods for Expected step traversal
        /// <summary>
        /// Flattens the expected trace hierarchy as much as possible
        /// Intended to simplify validation function and make expected
        /// trace more human-readable
        /// </summary>
        internal static void NormalizeExpectedTrace(TraceGroup currentTrace)
        {
            int index = 0;
            while (index < currentTrace.Steps.Count)
            {
                // overwrite if a placeholder
                if (currentTrace.Steps[index] is IPlaceholderTraceProvider)
                {
                    currentTrace.Steps[index] = ((IPlaceholderTraceProvider)currentTrace.Steps[index]).GetPlaceholderTrace();
                }

                if (currentTrace.Steps[index] is TraceGroup childStep)
                {
                    TraceValidator.NormalizeExpectedTrace(childStep);

                    if (childStep.Steps.Count == 0)
                    {
                        currentTrace.Steps.RemoveAt(index);
                        // skip index++;
                        continue;
                    }

                    if (childStep.ordered == currentTrace.ordered &&
                        !childStep.Optional &&
                        !childStep.Async)
                    {
                        //flatten the child step
                        currentTrace.Steps.RemoveAt(index);
                        currentTrace.Steps.InsertRange(index, childStep.Steps);
                        // count n steps forward and skip index++;
                        index = childStep.Steps.Count;
                        continue;
                    }
                }

                index++;
            }

            if ((currentTrace.Steps.Count == 1) &&
                (currentTrace.Steps[0] is TraceGroup))
            {
                // merge current step with the only child step, and their properties
                TraceGroup child = (TraceGroup)currentTrace.Steps[0];

                currentTrace.ordered = child.ordered;
                currentTrace.Async = (currentTrace.Async || child.Async);
                currentTrace.Optional = (currentTrace.Optional || child.Optional);

                //flatten the child step
                currentTrace.Steps.Clear();
                currentTrace.Steps.AddRange(child.Steps);
            }
        }


        /// <summary>
        /// Validate actual trace angainst expected trace.
        /// Call is comming from the parent step
        /// </summary>
        /// <param name="actualTrace">actual trace</param>
        /// <param name="startIndex">index to start searches from</param>
        /// <returns>true if traces match</returns>
        private static bool ValidateFirst(TraceGroup currentTrace, int startIndex)
        {
            currentTrace.startIndex = startIndex;
            for (int i = 0; i < currentTrace.Steps.Count; i++)
            {
                currentTrace.endIndexes[i] = -1;
            }

            bool match = false;

            if (!currentTrace.Optional)
            {
                match = TraceValidator.Validate(currentTrace, 0, startIndex, DateTime.MinValue, DateTime.MinValue);
            }
            else
            {
                TraceValidator.SetRestorePoint();
                match = TraceValidator.Validate(currentTrace, 0, startIndex, DateTime.MinValue, DateTime.MinValue);
                if (match)
                {
                    TraceValidator.Commit();
                }
                else
                {
                    TraceValidator.Rollback();

                    // try to skip this entire step, since it's optional
                    match = TraceValidator.ValidateNextSibling(currentTrace.parent, currentTrace.indexInParent, startIndex);
                }
            }

            return match;
        }

        /// <summary>
        /// Validate actual trace angainst expected trace.
        /// This method signals parent step to continue validation
        /// with the next sibling of this step.
        /// Call is comming from the child step
        /// </summary>
        /// <param name="childIndex">index of child just executed</param>
        /// <param name="actualTrace">actual trace</param>
        /// <param name="endIndex">index of the step next to the last step validated by child</param>
        /// <returns>true if traces match</returns>
        private static bool ValidateNextSibling(TraceGroup currentTrace, int childIndex, int endIndex)
        {
            currentTrace.endIndexes[childIndex] = endIndex;
            if (currentTrace.ordered)
            {
                return TraceValidator.Validate(currentTrace, childIndex + 1, endIndex, DateTime.MinValue, DateTime.MinValue);
            }
            else
            {
                return TraceValidator.Validate(currentTrace, childIndex + 1, currentTrace.startIndex, DateTime.MinValue, DateTime.MinValue);
            }
        }

        /// <summary>
        /// Validate actual trace angainst expected trace.
        /// This method validates next same level expected step
        /// </summary>
        /// <param name="curExpIndex">expected trace step index</param>
        /// <param name="actualTrace">actual trace</param>
        /// <param name="startIndex">index to start searches from</param>
        /// <param name="lastTime">time last step occured at</param>
        /// <param name="mustBeAfter">subsequent step must occur after time specified</param>
        /// <returns>true if traces match</returns>
        private static bool Validate(TraceGroup currentTrace, int curExpIndex, int startIndex, DateTime lastTime, DateTime mustBeAfter)
        {
            // is this a last step to check?
            if (curExpIndex >= currentTrace.Steps.Count)
            {
                bool match = false;

                if (null == currentTrace.parent)
                {
                    //Oracle.LogDebugInfo("End of step list. Verifying completeness");
                    return TraceValidator.VerifyAllStepsValidated();
                }
                else
                {
                    //Oracle.LogDebugInfo("Check next parent step");
                    if (currentTrace.Async)
                    {
                        match = TraceValidator.ValidateNextSibling(currentTrace.parent, currentTrace.indexInParent, currentTrace.startIndex);
                    }
                    else
                    {
                        if (currentTrace.ordered)
                        {
                            match = TraceValidator.ValidateNextSibling(currentTrace.parent, currentTrace.indexInParent, currentTrace.startIndex);
                        }
                        else
                        {
                            match = TraceValidator.ValidateNextSibling(currentTrace.parent, currentTrace.indexInParent, TraceValidator.GetMaxEndIndex(currentTrace));
                        }
                    }
                }

                return match;
            }

            WorkflowTraceStep step = currentTrace.Steps[curExpIndex];
            if (step is TraceGroup)
            {
                // check inner composite step
                return TraceValidator.ValidateFirst((TraceGroup)step, startIndex);
            }
            else if (step is DelayTrace)
            {
                mustBeAfter = lastTime.Add(((DelayTrace)step).TimeSpan);

                return TraceValidator.Validate(currentTrace, curExpIndex + 1, startIndex, lastTime, mustBeAfter);
            }
            else if (step is IActualTraceStep)
            {
                int[] entryIndexes = TraceValidator.FindAllEntries((IActualTraceStep)step, startIndex, mustBeAfter);
                bool match = false;

                if (entryIndexes.Length == 0)
                {
                    // step not found
                    if (!step.Optional)
                    {
                        string msg = String.Format("Step '{0}' is not found. Start index {1}", step, startIndex);
                        //Oracle.LogDebugInfo("Adding error: {0}", msg);
                        TraceValidator.s_errorList.Add(msg);
                    }
                }
                else if (entryIndexes.Length == 1 &&
                        !step.Optional)
                {
                    // this branch can be commented out
                    // it's an optimization for the most common case

                    // only one option
                    int index = entryIndexes[0];

                    TraceValidator.MarkAsFound(index, out lastTime);
                    currentTrace.endIndexes[curExpIndex] = index;

                    if (currentTrace.ordered && !step.Async)
                    {
                        match = TraceValidator.Validate(currentTrace, curExpIndex + 1, index + 1, lastTime, mustBeAfter);
                    }
                    else
                    {
                        match = TraceValidator.Validate(currentTrace, curExpIndex + 1, startIndex, lastTime, mustBeAfter);
                    }
                }
                else
                {
                    // many options. try each choice until succeed
                    foreach (int index in entryIndexes)
                    {
                        TraceValidator.SetRestorePoint();
                        //this.Dump("After SetRestorePoint");
                        TraceValidator.MarkAsFound(index, out lastTime);
                        currentTrace.endIndexes[curExpIndex] = index;
                        //this.Dump("After mark as found");

                        if (currentTrace.ordered && !step.Async)
                        {
                            match = TraceValidator.Validate(currentTrace, curExpIndex + 1, entryIndexes[0] + 1, lastTime, mustBeAfter);
                        }
                        else
                        {
                            match = TraceValidator.Validate(currentTrace, curExpIndex + 1, startIndex, lastTime, mustBeAfter);
                        }

                        //this.Dump("After searched subsequent steps");

                        if (match)
                        {
                            TraceValidator.Commit();
                            //this.Dump("After Commit");
                            break;
                        }
                        else
                        {
                            TraceValidator.Rollback();
                            //this.Dump("After Rollback1");
                        }
                    }
                }

                if (!match && step.Optional)
                {
                    //Oracle.LogDebugInfo("Skipping optional step");
                    match = TraceValidator.Validate(currentTrace, curExpIndex + 1, startIndex, lastTime, mustBeAfter);
                }

                return match;
            }
            else
            {
                throw new Exception(String.Format(
                    "Internal validation error. Unknown step type found {0}",
                    step.GetType().Name));
            }
        }

        private static int GetMaxEndIndex(TraceGroup currentTrace)
        {
            int max = 0;

            foreach (int n in currentTrace.endIndexes)
            {
                if (max < n)
                {
                    max = n;
                }
            }

            return max;
        }

        /// <summary>
        /// Verify all appropriate steps were marked as verified
        /// Exclude list from expected trace is used
        /// </summary>
        /// <returns></returns>
        private static bool VerifyAllStepsValidated()
        {
            bool result = true;

            if (TraceValidator.s_expectedTrace.verifyCompleteness)
            {
                int index = 0;

                foreach (IActualTraceStep step in TraceValidator.s_actualTrace.Steps)
                {
                    if (step.Validated == 0)
                    {
                        string errMsg = String.Format("Unexpected step found {0}, index {1}", step, index);
                        TraceValidator.s_errorList.Add(errMsg);
                        result = false;
                        break;
                    }
                    index++;
                }
            }

            return result;
        }

        #endregion

        #region Step counter

        private static void AddActualStepCount(IActualTraceStep step)
        {
            TraceValidator.GetStepCount(step).actualTraceCount++;
        }

        private static void AddExpectedReqStepCount(IActualTraceStep step)
        {
            TraceValidator.GetStepCount(step).expectedReqCount++;
        }

        private static void AddExpectedOptStepCount(IActualTraceStep step)
        {
            TraceValidator.GetStepCount(step).expectedOptCount++;
        }

        /// <summary>
        /// Verify actual and expected counts correspond to each other
        /// Throws exception if validation failed
        /// </summary>
        /// <param name="expectedTrace">expected trace</param>
        /// <returns>true is count validation is successful</returns>
        private static void CheckStepCounts()
        {
            foreach (StepCount stepCnt in TraceValidator.s_stepCounts.Values)
            {
                if ((stepCnt.actualTraceCount > (stepCnt.expectedReqCount + stepCnt.expectedOptCount)) &&
                    TraceValidator.s_expectedTrace.verifyCompleteness)
                {
                    string errMsg = String.Format(
                            "Unexpected step(s) found {0} (actual {1}/req {2}/opt {3})",
                            stepCnt.step,
                            stepCnt.actualTraceCount,
                            stepCnt.expectedReqCount,
                            stepCnt.expectedOptCount);

                    //LogDebugInfo("Adding error: {0}", errMsg);
                    TraceValidator.s_errorList.Add(errMsg);
                }

                if (stepCnt.actualTraceCount < stepCnt.expectedReqCount)
                {
                    string errMsg = String.Format(
                        "Step(s) is not found {0} (actual {1}/req {2}/opt {3})",
                        stepCnt.step,
                        stepCnt.actualTraceCount,
                        stepCnt.expectedReqCount,
                        stepCnt.expectedOptCount);

                    //LogDebugInfo("Adding error: {0}", errMsg);
                    TraceValidator.s_errorList.Add(errMsg);
                }
            }
        }

        private static StepCount GetStepCount(IActualTraceStep step)
        {
            // build step string ID
            string stepId = step.GetStringId();

            if (!TraceValidator.s_stepCounts.ContainsKey(stepId))
            {
                TraceValidator.s_stepCounts.Add(stepId, new StepCount(step));
            }

            StepCount stepCount = TraceValidator.s_stepCounts[stepId];
            return stepCount;
        }

        private class StepCount
        {
            public int actualTraceCount = 0;
            public int expectedReqCount = 0;
            public int expectedOptCount = 0;

            public IActualTraceStep step;

            public StepCount(IActualTraceStep step)
            {
                this.step = step;
            }
        }
        #endregion
    }
}

