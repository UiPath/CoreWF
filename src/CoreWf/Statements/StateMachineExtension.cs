// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Hosting;
using CoreWf.Runtime;
using System;
using System.Collections.Generic;

namespace CoreWf.Statements
{
    /// <summary>
    /// StateMachineExtension is used to resume a bookmark outside StateMachine.
    /// </summary>
    internal class StateMachineExtension : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy _instance;

        /// <summary>
        /// Used to get additional extensions.
        /// </summary>
        /// <returns>Returns a IEnumerable of extensions</returns>
        public IEnumerable<object> GetAdditionalExtensions()
        {
            return null;
        }

        /// <summary>
        /// called with the targe instance under WorkflowInstance.Initialize
        /// </summary>
        /// <param name="instance">The value of WorkflowInstanceProxy</param>
        public void SetInstance(WorkflowInstanceProxy instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// Used to resume bookmark outside workflow.
        /// </summary>
        /// <param name="bookmark">The value of Bookmark to be resumed</param>
        public void ResumeBookmark(Bookmark bookmark)
        {
            IAsyncResult asyncResult = _instance.BeginResumeBookmark(bookmark, null, Fx.ThunkCallback(new AsyncCallback(StateMachineExtension.OnResumeBookmarkCompleted)), _instance);
            if (asyncResult.CompletedSynchronously)
            {
                _instance.EndResumeBookmark(asyncResult);
            }
        }

        private static void OnResumeBookmarkCompleted(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                WorkflowInstanceProxy instance = result.AsyncState as WorkflowInstanceProxy;
                Fx.Assert(instance != null, "BeginResumeBookmark should pass a WorkflowInstanceProxy object as the async state object.");
                instance.EndResumeBookmark(result);
            }
        }
    }
}
