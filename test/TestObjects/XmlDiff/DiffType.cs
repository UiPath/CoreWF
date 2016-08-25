// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Common.TestObjects.XmlDiff
{
    internal enum DiffType
    {
        None,
        Success,
        Element,
        Whitespace,
        Comment,
        PI,
        Text,
        CData,
        Attribute,
        NS,
        Prefix,
        SourceExtra,
        TargetExtra,
        NodeType
    }
}
