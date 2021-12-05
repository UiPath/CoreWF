// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public delegate void CompletionCallback(NativeActivityContext context, ActivityInstance completedInstance);
public delegate void CompletionCallback<TResult>(NativeActivityContext context, ActivityInstance completedInstance, TResult result);
