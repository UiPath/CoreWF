// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.DynamicUpdate
{
    using System;
    
    static class ErrorCodes
    {
        public const string ArgumentsChanged = "0100";
        public const string CannotCreateEnvironment = "0101";
        public const string CannotAddHandles = "0102";
        public const string EnvironmentIndexOutOfRange = "0103";
        public const string CannotRemoveExecutingActivity = "0104";
        public const string MapIdInvalid = "0105";
        public const string InvalidActivityId = "0106";
        public const string MissingMap = "0107";
        public const string UpdateBlockedInside = "0108";
        public const string DisallowedForActivitySpecificReason = "0109";
        public const string CannotUpdateEnvironmentInTheMiddleOfResolvingVariables = "0110";
        public const string NativeActivityUpdateInstanceThrewException = "0111";
    }
}
