// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xaml;
using System.Xaml.Schema;

namespace System.Activities.Debugger;

public class XamlDebuggerXmlReader : XamlReader, IXamlLineInfo
{
    private const string StartLineMemberName = "StartLine";
    private const string StartColumnMemberName = "StartColumn";
    private const string EndLineMemberName = "EndLine";
    private const string EndColumnMemberName = "EndColumn";

    private const string FileNameMemberName = "FileName";

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier StartLineName =
        new(typeof(XamlDebuggerXmlReader), StartLineMemberName);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier StartColumnName =
        new(typeof(XamlDebuggerXmlReader), StartColumnMemberName);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier EndLineName = new(typeof(XamlDebuggerXmlReader),
        EndLineMemberName);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier EndColumnName =
        new(typeof(XamlDebuggerXmlReader), EndColumnMemberName);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier FileNameName = new(typeof(XamlDebuggerXmlReader),
        FileNameMemberName);

    private static readonly Type s_attachingType = typeof(XamlDebuggerXmlReader);

    private static readonly MethodInfo s_startLineGetterMethodInfo =
        s_attachingType.GetMethod("GetStartLine", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_startLineSetterMethodInfo =
        s_attachingType.GetMethod("SetStartLine", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_startColumnGetterMethodInfo =
        s_attachingType.GetMethod("GetStartColumn", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_startColumnSetterMethodInfo =
        s_attachingType.GetMethod("SetStartColumn", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_endLineGetterMethodInfo =
        s_attachingType.GetMethod("GetEndLine", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_endLineSetterMethodInfo =
        s_attachingType.GetMethod("SetEndLine", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_endColumnGetterMethodInfo =
        s_attachingType.GetMethod("GetEndColumn", BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo s_endColumnSetterMethodInfo =
        s_attachingType.GetMethod("SetEndColumn", BindingFlags.Public | BindingFlags.Static);

    private readonly Queue<XamlNode> _bufferedXamlNodes;
    private readonly Dictionary<XamlNode, DocumentRange> _initializationValueRanges;
    private readonly Stack<XamlNode> _objectDeclarationRecords;
    private readonly XamlSchemaContext _schemaContext;
    private readonly IXamlLineInfo _xamlLineInfo;
    private XamlMember _endColumnMember;
    private XamlMember _endLineMember;
    private XamlSourceLocationCollector _sourceLocationCollector;
    private XamlMember _startColumnMember;

    private XamlMember _startLineMember;
    private int _suppressMarkupExtensionLevel;
    private XamlReader _underlyingReader;
    private XmlReaderWithSourceLocation _xmlReaderWithSourceLocation;

    public XamlDebuggerXmlReader(TextReader underlyingTextReader)
        : this(underlyingTextReader, new XamlSchemaContext()) { }

    public XamlDebuggerXmlReader(TextReader underlyingTextReader, XamlSchemaContext schemaContext)
        : this(underlyingTextReader, schemaContext, null) { }

    internal XamlDebuggerXmlReader(TextReader underlyingTextReader, XamlSchemaContext schemaContext,
        Assembly localAssembly)
    {
        UnitTestUtility.Assert(underlyingTextReader != null,
            "underlyingTextReader should not be null and is ensured by caller.");
        _xmlReaderWithSourceLocation = new XmlReaderWithSourceLocation(underlyingTextReader);
        _underlyingReader = new XamlXmlReader(_xmlReaderWithSourceLocation, schemaContext,
            new XamlXmlReaderSettings {ProvideLineInfo = true, LocalAssembly = localAssembly});
        _xamlLineInfo = (IXamlLineInfo) _underlyingReader;
        UnitTestUtility.Assert(_xamlLineInfo.HasLineInfo,
            "underlyingReader is constructed with the ProvideLineInfo option above.");
        _schemaContext = schemaContext;
        _objectDeclarationRecords = new Stack<XamlNode>();
        _initializationValueRanges = new Dictionary<XamlNode, DocumentRange>();
        _bufferedXamlNodes = new Queue<XamlNode>();
        Current = CreateCurrentNode();
        SourceLocationFound += SetSourceLocation;
    }

    // A XamlReader that need to collect source level information is necessary
    // the one that is closest to the source document.
    // This constructor is fundamentally flawed because it allows any XAML reader
    // Which could output some XAML node that does not correspond to source.
    [Obsolete(
        "Don't use this constructor. Use \"public XamlDebuggerXmlReader(TextReader underlyingTextReader)\" or \"public XamlDebuggerXmlReader(TextReader underlyingTextReader, XamlSchemaContext schemaContext)\" instead.")]
    public XamlDebuggerXmlReader(XamlReader underlyingReader, TextReader textReader)
        : this(underlyingReader, underlyingReader as IXamlLineInfo, textReader) { }

    // This one is worse because in implementation we expect the same object instance through two parameters.
    [Obsolete(
        "Don't use this constructor. Use \"public XamlDebuggerXmlReader(TextReader underlyingTextReader)\" or \"public XamlDebuggerXmlReader(TextReader underlyingTextReader, XamlSchemaContext schemaContext)\" instead.")]
    public XamlDebuggerXmlReader(XamlReader underlyingReader, IXamlLineInfo xamlLineInfo, TextReader textReader)
    {
        _underlyingReader = underlyingReader;
        _xamlLineInfo = xamlLineInfo;
        _xmlReaderWithSourceLocation = new XmlReaderWithSourceLocation(textReader);
        _initializationValueRanges = new Dictionary<XamlNode, DocumentRange>();
        // Parse the XML at once to get all the locations we wanted.
        while (_xmlReaderWithSourceLocation.Read()) { }

        _schemaContext = underlyingReader.SchemaContext;
        _objectDeclarationRecords = new Stack<XamlNode>();
        _bufferedXamlNodes = new Queue<XamlNode>();
        Current = CreateCurrentNode();
        SourceLocationFound += SetSourceLocation;
    }

    public bool CollectNonActivitySourceLocation { get; set; }

    public override XamlNodeType NodeType => Current.NodeType;

    public override XamlType Type => Current.Type;

    public override XamlMember Member => Current.Member;

    public override object Value => Current.Value;

    public override bool IsEof => _underlyingReader.IsEof;

    public override NamespaceDeclaration Namespace => Current.Namespace;

    public override XamlSchemaContext SchemaContext => _schemaContext;

    internal XamlMember StartLineMember
    {
        get
        {
            _startLineMember ??= CreateAttachableMember(s_startLineGetterMethodInfo, s_startLineSetterMethodInfo,
                SourceLocationMemberType.StartLine);
            return _startLineMember;
        }
    }

    internal XamlMember StartColumnMember
    {
        get
        {
            _startColumnMember ??= CreateAttachableMember(s_startColumnGetterMethodInfo, s_startColumnSetterMethodInfo,
                SourceLocationMemberType.StartColumn);
            return _startColumnMember;
        }
    }

    internal XamlMember EndLineMember
    {
        get
        {
            _endLineMember ??= CreateAttachableMember(s_endLineGetterMethodInfo, s_endLineSetterMethodInfo,
                SourceLocationMemberType.EndLine);
            return _endLineMember;
        }
    }

    internal XamlMember EndColumnMember
    {
        get
        {
            _endColumnMember ??= CreateAttachableMember(s_endColumnGetterMethodInfo, s_endColumnSetterMethodInfo,
                SourceLocationMemberType.EndColumn);
            return _endColumnMember;
        }
    }

    private XamlNode Current { get; set; }

    private XamlSourceLocationCollector SourceLocationCollector
    {
        get
        {
            _sourceLocationCollector ??= new XamlSourceLocationCollector(this);
            return _sourceLocationCollector;
        }
    }

    public bool HasLineInfo => true;

    public int LineNumber => Current.LineNumber;

    public int LinePosition => Current.LinePosition;

    public event EventHandler<SourceLocationFoundEventArgs> SourceLocationFound
    {
        add => _sourceLocationFound += value;
        remove => _sourceLocationFound -= value;
    }

    private event EventHandler<SourceLocationFoundEventArgs> _sourceLocationFound;

    [Fx.Tag.InheritThrowsAttribute(From = "TryGetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    private static int GetIntegerAttachedProperty(object instance, AttachableMemberIdentifier memberIdentifier)
    {
        if (AttachablePropertyServices.TryGetProperty(instance, memberIdentifier, out int value))
        {
            return value;
        }

        return -1;
    }

    [Fx.Tag.InheritThrowsAttribute(From = "GetIntegerAttachedProperty")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static object GetStartLine(object instance)
    {
        return GetIntegerAttachedProperty(instance, StartLineName);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetStartLine(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, StartLineName, value);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "GetIntegerAttachedProperty")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static object GetStartColumn(object instance)
    {
        return GetIntegerAttachedProperty(instance, StartColumnName);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetStartColumn(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, StartColumnName, value);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "GetIntegerAttachedProperty")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static object GetEndLine(object instance)
    {
        return GetIntegerAttachedProperty(instance, EndLineName);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetEndLine(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, EndLineName, value);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "GetIntegerAttachedProperty")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static object GetEndColumn(object instance)
    {
        return GetIntegerAttachedProperty(instance, EndColumnName);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetEndColumn(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, EndColumnName, value);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetFileName(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, FileNameName, value);
    }

    [Fx.Tag.InheritThrowsAttribute(From = "TryGetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static object GetFileName(object instance)
    {
        return AttachablePropertyServices.TryGetProperty(instance, FileNameName, out string value) ? value : string.Empty;
    }

    // Copy source location information from source to destination (if available)
    public static void CopyAttachedSourceLocation(object source, object destination)
    {
        if (AttachablePropertyServices.TryGetProperty(source, StartLineName, out int startLine) &&
            AttachablePropertyServices.TryGetProperty(source, StartColumnName, out int startColumn) &&
            AttachablePropertyServices.TryGetProperty(source, EndLineName, out int endLine) &&
            AttachablePropertyServices.TryGetProperty(source, EndColumnName, out int endColumn))
        {
            SetStartLine(destination, startLine);
            SetStartColumn(destination, startColumn);
            SetEndLine(destination, endLine);
            SetEndColumn(destination, endColumn);
        }
    }

    internal static void SetSourceLocation(object sender, SourceLocationFoundEventArgs args)
    {
        var target = args.Target;
        var targetType = target.GetType();
        var reader = (XamlDebuggerXmlReader) sender;
        var shouldStoreAttachedProperty = false;

        if (reader.CollectNonActivitySourceLocation)
        {
            shouldStoreAttachedProperty = targetType != typeof(string);
        }
        else
        {
            if (typeof(Activity).IsAssignableFrom(targetType) &&
                !typeof(IExpressionContainer).IsAssignableFrom(targetType) &&
                !typeof(IValueSerializableExpression).IsAssignableFrom(targetType))
            {
                shouldStoreAttachedProperty = true;
            }
        }

        shouldStoreAttachedProperty = shouldStoreAttachedProperty && !args.IsValueNode;

        if (shouldStoreAttachedProperty)
        {
            var sourceLocation = args.SourceLocation;
            SetStartLine(target, sourceLocation.StartLine);
            SetStartColumn(target, sourceLocation.StartColumn);
            SetEndLine(target, sourceLocation.EndLine);
            SetEndColumn(target, sourceLocation.EndColumn);
        }
    }

    public override bool Read()
    {
        bool readSucceed;
        if (_bufferedXamlNodes.Count > 0)
        {
            Current = _bufferedXamlNodes.Dequeue();
            readSucceed = Current != null;
        }
        else
        {
            readSucceed = _underlyingReader.Read();
            if (!readSucceed)
            {
                return false;
            }

            Current = CreateCurrentNode(_underlyingReader, _xamlLineInfo);
            PushObjectDeclarationNodeIfApplicable();
            switch (Current.NodeType)
            {
                case XamlNodeType.StartMember:

                    // When we reach a StartMember node, the next node to come might be a Value.
                    // To correctly pass SourceLocation information, we need to rewrite this node to use ValueNodeXamlMemberInvoker.
                    // But we don't know if the next node is a Value node yet, so we are buffering here and look ahead for a single node.
                    UnitTestUtility.Assert(_bufferedXamlNodes.Count == 0,
                        "this.bufferedXamlNodes should be empty when we reach this code path.");
                    _bufferedXamlNodes.Enqueue(Current);

                    // This directive represents the XAML node or XAML information set 
                    // representation of initialization text, where a string within an 
                    // object element supplies the type construction information for 
                    // the surrounding object element.
                    var isInitializationValue = Current.Member == XamlLanguage.Initialization;

                    var moreNode = _underlyingReader.Read();
                    UnitTestUtility.Assert(moreNode, "Start Member must followed by some other nodes.");

                    Current = CreateCurrentNode();

                    _bufferedXamlNodes.Enqueue(Current);

                    // It is possible that the next node after StartMember is a StartObject/GetObject.
                    // We need to push the object declaration node to the Stack
                    PushObjectDeclarationNodeIfApplicable();

                    if (!SuppressingMarkupExtension()
                        && Current.NodeType == XamlNodeType.Value)
                    {
                        var currentLocation = new DocumentLocation(Current.LineNumber, Current.LinePosition);
                        var isInAttribute =
                            _xmlReaderWithSourceLocation.AttributeValueRanges.TryGetValue(currentLocation,
                                out var valueRange);
                        var isInContent = !isInAttribute &&
                            _xmlReaderWithSourceLocation.ContentValueRanges.TryGetValue(currentLocation,
                                out valueRange);

                        if (isInAttribute || isInContent && !isInitializationValue)
                        {
                            // For Value Node with known line info, we want to route the value setting process through this Reader.
                            // Therefore we need to go back to the member node and replace the XamlMemberInvoker.
                            var startMemberNodeForValue = _bufferedXamlNodes.Peek();
                            var xamlMemberForValue = startMemberNodeForValue.Member;
                            XamlMemberInvoker newXamlMemberInvoker =
                                new ValueNodeXamlMemberInvoker(this, xamlMemberForValue.Invoker, valueRange);
                            startMemberNodeForValue.Member =
                                xamlMemberForValue.ReplaceXamlMemberInvoker(_schemaContext, newXamlMemberInvoker);
                        }
                        else if (isInContent && isInitializationValue)
                        {
                            var currentStartObject = _objectDeclarationRecords.Peek();

                            if (!_initializationValueRanges.ContainsKey(currentStartObject))
                            {
                                _initializationValueRanges.Add(currentStartObject, valueRange);
                            }
                            else
                            {
                                UnitTestUtility.Assert(false,
                                    "I assume it is impossible for an object  to have more than one initialization member");
                            }
                        }
                    }

                    StartAccessingBuffer();
                    break;

                case XamlNodeType.EndObject:

                    InjectLineInfoXamlNodesToBuffer();
                    StartAccessingBuffer();
                    break;

                case XamlNodeType.Value:
                    break;
            }
        }

        return readSucceed;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ((IDisposable) _underlyingReader)?.Dispose();

            _underlyingReader = null;

            ((IDisposable) _xmlReaderWithSourceLocation)?.Dispose();

            _xmlReaderWithSourceLocation = null;
        }
    }

    private static XamlNode CreateCurrentNode(XamlReader xamlReader, IXamlLineInfo xamlLineInfo)
    {
        var currentNode = new XamlNode
        {
            Namespace = xamlReader.Namespace,
            NodeType = xamlReader.NodeType,
            Type = xamlReader.Type,
            Member = xamlReader.Member,
            Value = xamlReader.Value,
            LineNumber = xamlLineInfo.LineNumber,
            LinePosition = xamlLineInfo.LinePosition
        };

        return currentNode;
    }

    private static bool IsMarkupExtension(XamlNode node)
    {
        Fx.Assert(node != null, "node != null");
        return node?.Type != null && node.Type.IsMarkupExtension;
    }

    private bool SuppressingMarkupExtension() => _suppressMarkupExtensionLevel != 0;

    private XamlNode CreateCurrentNode() => CreateCurrentNode(_underlyingReader, _xamlLineInfo);

    private void StartAccessingBuffer() => Current = _bufferedXamlNodes.Dequeue();

    private void PushObjectDeclarationNodeIfApplicable()
    {
        switch (Current.NodeType)
        {
            case XamlNodeType.StartObject:
            case XamlNodeType.GetObject:
                _objectDeclarationRecords.Push(Current);
                if (IsMarkupExtension(Current))
                {
                    ++_suppressMarkupExtensionLevel;
                }

                break;
        }
    }

    private void OnValueNodeDeserialized(object value, DocumentRange attributeValueLocation)
    {
        var startLine = attributeValueLocation.Start.LineNumber.Value;
        var startColumn = attributeValueLocation.Start.LinePosition.Value;
        var endLine = attributeValueLocation.End.LineNumber.Value;
        var endColumn = attributeValueLocation.End.LinePosition.Value;
        // XamlDebuggerXmlReader has no idea what the filename is (it only knew a stream of data)
        // So we set FileName = null.

        // To enhance visual selection, endColumn + 1
        var valueLocation = new SourceLocation(null, startLine, startColumn, endLine, endColumn + 1);
        NotifySourceLocationFound(value, valueLocation, true);
    }

    private void InjectLineInfoXamlNodesToBuffer()
    {
        var startNode = _objectDeclarationRecords.Pop();

        if (!SuppressingMarkupExtension() && startNode.Type != null && !startNode.Type.IsUnknown &&
            !startNode.Type.IsMarkupExtension)
        {
            DocumentLocation myStartBracket = null;
            DocumentLocation myEndBracket = null;
            var myStartLocation = new DocumentLocation(startNode.LineNumber, startNode.LinePosition);
            if (_xmlReaderWithSourceLocation.EmptyElementRanges.TryGetValue(myStartLocation, out var myRange))
            {
                myStartBracket = myRange.Start;
                myEndBracket = myRange.End;
            }
            else
            {
                var myEndLocation = new DocumentLocation(Current.LineNumber, Current.LinePosition);
                _xmlReaderWithSourceLocation.StartElementLocations.TryGetValue(myStartLocation, out myStartBracket);
                _xmlReaderWithSourceLocation.EndElementLocations.TryGetValue(myEndLocation, out myEndBracket);
            }

            // To enhance visual selection
            var myRealEndBracket =
                new DocumentLocation(myEndBracket.LineNumber.Value, myEndBracket.LinePosition.Value + 1);

            _bufferedXamlNodes.Clear();
            InjectLineInfoMembersToBuffer(myStartBracket, myRealEndBracket);

            if (_initializationValueRanges.TryGetValue(startNode, out var valueRange))
            {
                var realValueRange = new DocumentRange(valueRange.Start,
                    new DocumentLocation(valueRange.End.LineNumber.Value, valueRange.End.LinePosition.Value + 1));
                SourceLocationCollector.AddValueRange(new DocumentRange(myStartBracket, myRealEndBracket),
                    realValueRange);
            }
        }

        if (IsMarkupExtension(startNode))
        {
            // Pop a level
            Fx.Assert(_suppressMarkupExtensionLevel > 0, "this.suppressMarkupExtensionLevel > 0");
            --_suppressMarkupExtensionLevel;
        }

        // We need to make sure we also buffer the current node so that this is not missed when the buffer exhausts.
        _bufferedXamlNodes.Enqueue(Current);
    }

    private void InjectLineInfoMembersToBuffer(DocumentLocation startPosition, DocumentLocation endPosition)
    {
        InjectLineInfoMemberToBuffer(StartLineMember, startPosition.LineNumber.Value);
        InjectLineInfoMemberToBuffer(StartColumnMember, startPosition.LinePosition.Value);
        InjectLineInfoMemberToBuffer(EndLineMember, endPosition.LineNumber.Value);
        InjectLineInfoMemberToBuffer(EndColumnMember, endPosition.LinePosition.Value);
    }

    private void InjectLineInfoMemberToBuffer(XamlMember member, int value)
    {
        _bufferedXamlNodes.Enqueue(new XamlNode {NodeType = XamlNodeType.StartMember, Member = member});
        _bufferedXamlNodes.Enqueue(new XamlNode {NodeType = XamlNodeType.Value, Value = value});
        _bufferedXamlNodes.Enqueue(new XamlNode {NodeType = XamlNodeType.EndMember, Member = member});
    }

    private XamlMember CreateAttachableMember(MethodInfo getter, MethodInfo setter, SourceLocationMemberType memberType)
    {
        var memberName = memberType.ToString();
        var invoker = new SourceLocationMemberInvoker(SourceLocationCollector, memberType);
        return new XamlMember(memberName, getter, setter, _schemaContext, invoker);
    }

    private void NotifySourceLocationFound(object instance, SourceLocation currentLocation, bool isValueNode)
    {
        // For Argument containing an IValueSerializable expression serializing as a ValueNode.
        // We associate the SourceLocation to the expression instead of the Argument.
        // For example, when we have <WriteLine Text="[abc]" />, Then the SourceLocation found for the InArgument object 
        // is associated with the VisualBasicValue object instead.
        if (isValueNode && instance is Argument {Expression: IValueSerializableExpression} argumentInstance)
        {
            instance = argumentInstance.Expression;
        }

        _sourceLocationFound?.Invoke(this, new SourceLocationFoundEventArgs(instance, currentLocation, isValueNode));
    }

    private class XamlSourceLocationCollector
    {
        private readonly Dictionary<DocumentRange, DocumentRange> _objRgnToInitValueRgnMapping;
        private readonly XamlDebuggerXmlReader _parent;
        private object _currentObject;
        private int _endColumn;
        private int _endLine;
        private int _startColumn;
        private int _startLine;

        internal XamlSourceLocationCollector(XamlDebuggerXmlReader parent)
        {
            _parent = parent;
            _objRgnToInitValueRgnMapping = new Dictionary<DocumentRange, DocumentRange>();
        }

        internal void OnStartLineFound(object instance, int value)
        {
            UnitTestUtility.Assert(_currentObject == null,
                "This should be ensured by the XamlSourceLocationObjectReader to emit attachable property in proper order");
            _currentObject = instance;
            _startLine = value;
        }

        internal void OnStartColumnFound(object instance, int value)
        {
            UnitTestUtility.Assert(instance == _currentObject,
                "This should be ensured by the XamlSourceLocationObjectReader to emit attachable property in proper order");
            _startColumn = value;
        }

        internal void OnEndLineFound(object instance, int value)
        {
            UnitTestUtility.Assert(instance == _currentObject,
                "This should be ensured by the XamlSourceLocationObjectReader to emit attachable property in proper order");
            _endLine = value;
        }

        internal void OnEndColumnFound(object instance, int value)
        {
            UnitTestUtility.Assert(instance == _currentObject,
                "This should be ensured by the XamlSourceLocationObjectReader to emit attachable property in proper order");
            _endColumn = value;

            // Notify value first to keep the order from "inner to outer".
            NotifyValueIfNeeded(instance);

            // XamlDebuggerXmlReader has no idea what the filename is (it only knew a stream of data)
            // So we set FileName = null.
            _parent.NotifySourceLocationFound(instance,
                new SourceLocation( /* FileName = */ null, _startLine, _startColumn, _endLine, _endColumn), false);
            _currentObject = null;
        }

        internal void AddValueRange(DocumentRange startNodeRange, DocumentRange valueRange) =>
            _objRgnToInitValueRgnMapping.Add(startNodeRange, valueRange);

        private static bool ShouldReportValue(object instance) => instance is Argument;

        // in the case:
        // <InArgument x:TypeArguments="x:String">["abc" + ""]</InArgument>
        // instance is a Argument, with a VB Expression.
        // We hope, the VB expression got notified, too.
        private void NotifyValueIfNeeded(object instance)
        {
            if (!ShouldReportValue(instance))
            {
                return;
            }

            if (_objRgnToInitValueRgnMapping.TryGetValue(
                    new DocumentRange(_startLine, _startColumn, _endLine, _endColumn), out var valueRange))
            {
                _parent.NotifySourceLocationFound(instance,
                    new SourceLocation( /* FileName = */ null,
                        valueRange.Start.LineNumber.Value,
                        valueRange.Start.LinePosition.Value,
                        valueRange.End.LineNumber.Value,
                        valueRange.End.LinePosition.Value),
                    true);
            }
        }
    }

    private class SourceLocationMemberInvoker : XamlMemberInvoker
    {
        private readonly XamlSourceLocationCollector _sourceLocationCollector;
        private readonly SourceLocationMemberType _sourceLocationMember;

        public SourceLocationMemberInvoker(XamlSourceLocationCollector sourceLocationCollector,
            SourceLocationMemberType sourceLocationMember)
        {
            _sourceLocationCollector = sourceLocationCollector;
            _sourceLocationMember = sourceLocationMember;
        }

        public override object GetValue(object instance)
        {
            UnitTestUtility.Assert(false, "This method should not be called within framework code.");
            return null;
        }

        public override void SetValue(object instance, object propertyValue)
        {
            UnitTestUtility.Assert(propertyValue is int,
                "The value for this attachable property should be an integer and is ensured by the emitter.");
            var value = (int) propertyValue;
            switch (_sourceLocationMember)
            {
                case SourceLocationMemberType.StartLine:
                    _sourceLocationCollector.OnStartLineFound(instance, value);
                    break;
                case SourceLocationMemberType.StartColumn:
                    _sourceLocationCollector.OnStartColumnFound(instance, value);
                    break;
                case SourceLocationMemberType.EndLine:
                    _sourceLocationCollector.OnEndLineFound(instance, value);
                    break;
                case SourceLocationMemberType.EndColumn:
                    _sourceLocationCollector.OnEndColumnFound(instance, value);
                    break;
                default:
                    UnitTestUtility.Assert(false, "All possible SourceLocationMember are exhausted.");
                    break;
            }
        }
    }

    private class ValueNodeXamlMemberInvoker : XamlMemberInvoker
    {
        private readonly DocumentRange _attributeValueRange;
        private readonly XamlDebuggerXmlReader _parent;
        private readonly XamlMemberInvoker _wrapped;

        internal ValueNodeXamlMemberInvoker(XamlDebuggerXmlReader parent, XamlMemberInvoker wrapped,
            DocumentRange attributeValueRange)
        {
            _parent = parent;
            _wrapped = wrapped;
            _attributeValueRange = attributeValueRange;
        }

        public override ShouldSerializeResult ShouldSerializeValue(object instance) =>
            _wrapped.ShouldSerializeValue(instance);

        public override object GetValue(object instance) => _wrapped.GetValue(instance);

        public override void SetValue(object instance, object value)
        {
            _parent.OnValueNodeDeserialized(value, _attributeValueRange);
            _wrapped.SetValue(instance, value);
        }
    }
}
