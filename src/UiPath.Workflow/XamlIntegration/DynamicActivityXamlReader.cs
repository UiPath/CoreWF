// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Xaml;
using System.Xaml.Schema;

namespace System.Activities.XamlIntegration;

// This Xaml Reader converts an <Activity x:Class=Foo to <DynamicActivity Name=Foo
// it does the folowing
// Rewrites any record of type "Activity" to "DynamicActivity"
// Rewrites any member of type "Activity" to member "DynamicActivity"
// Rewrites x:Class to DynamicActivity.Name
// Recognizes DynamicActivity<T>.
//
// This Xaml Reader also supports ActivityBuilder, which has the same basic node structure
internal class DynamicActivityXamlReader : XamlReader, IXamlLineInfo
{
    private const string ClrNamespacePart = "clr-namespace:";
    internal static readonly XamlMember XPropertyType = XamlLanguage.Property.GetMember("Type");
    internal static readonly XamlMember XPropertyName = XamlLanguage.Property.GetMember("Name");
    internal static readonly XamlMember XPropertyAttributes = XamlLanguage.Property.GetMember("Attributes");
    private readonly XamlMember _activityPropertyAttributes;
    private readonly XamlMember _activityPropertyName;
    private readonly XamlMember _activityPropertyType;
    private readonly XamlMember _activityPropertyValue;
    private readonly XamlType _activityPropertyXamlType;
    private readonly XamlType _baseActivityXamlType;
    private readonly XamlReader _innerReader;
    private readonly IXamlLineInfo _innerReaderLineInfo;
    private readonly bool _isBuilder;
    private readonly NamespaceTable _namespaceTable;

    // we pull off of the innerReader and into this nodeList, where we use its reader
    private readonly XamlNodeQueue _nodeQueue;
    private readonly XamlSchemaContext _schemaContext;
    private readonly XamlType _typeXamlType;
    private readonly XamlType _xamlTypeXamlType;

    // These may be a closed generic types in the Activity<T> case, so we compute them dynamically
    private XamlType _activityReplacementXamlType;
    private XamlType _activityXamlType;

    // Properties are tricky since they support default values, and those values
    // can appear anywhere in the XAML document. So we need to buffer their XAML 
    // nodes and present them only at the end of the document (right before the 
    // document end tag), when we have both the declaration and the value realized.
    private BufferedPropertyList _bufferedProperties;

    // in the ActivityBuilder case we need to jump through some extra hoops to 
    // support PropertyReferenceExtension, since in the ActivityBuilder case
    // Implementation isn't a template (Func<Activity>), so we need to map
    // such members into attached properties on their parent object
    private BuilderStack _builderStack;
    private int _depth;
    private bool _frontLoadedDirectives;
    private int _inXClassDepth;
    private XamlReader _nodeReader;
    private IXamlLineInfo _nodeReaderLineInfo;
    private bool _notRewriting;
    private XamlTypeName _xClassName;

    public DynamicActivityXamlReader(XamlReader innerReader)
        : this(innerReader, null) { }

    public DynamicActivityXamlReader(XamlReader innerReader, XamlSchemaContext schemaContext)
        : this(false, innerReader, schemaContext) { }

    public DynamicActivityXamlReader(bool isBuilder, XamlReader innerReader, XamlSchemaContext schemaContext)
    {
        _isBuilder = isBuilder;
        _innerReader = innerReader;
        _schemaContext = schemaContext ?? innerReader.SchemaContext;

        _xamlTypeXamlType = _schemaContext.GetXamlType(typeof(XamlType));
        _typeXamlType = _schemaContext.GetXamlType(typeof(Type));

        _baseActivityXamlType = _schemaContext.GetXamlType(typeof(Activity));
        _activityPropertyXamlType = _schemaContext.GetXamlType(typeof(DynamicActivityProperty));
        _activityPropertyType = _activityPropertyXamlType.GetMember("Type");
        _activityPropertyName = _activityPropertyXamlType.GetMember("Name");
        _activityPropertyValue = _activityPropertyXamlType.GetMember("Value");
        _activityPropertyAttributes = _activityPropertyXamlType.GetMember("Attributes");

        _namespaceTable = new NamespaceTable();
        _frontLoadedDirectives = true;

        // we pump items through this node-list when rewriting
        _nodeQueue = new XamlNodeQueue(_schemaContext);
        _nodeReader = _nodeQueue.Reader;
        if (innerReader is IXamlLineInfo {HasLineInfo: true} lineInfo)
        {
            _innerReaderLineInfo = lineInfo;
            _nodeReaderLineInfo = (IXamlLineInfo) _nodeQueue.Reader;
            HasLineInfo = true;
        }
    }

    public override XamlType Type => _nodeReader.Type;

    public override NamespaceDeclaration Namespace => _nodeReader.Namespace;

    public override object Value => _nodeReader.Value;

    public override bool IsEof => _nodeReader.IsEof;

    public override XamlMember Member => _nodeReader.Member;

    public override XamlSchemaContext SchemaContext => _schemaContext;

    public override XamlNodeType NodeType => _nodeReader.NodeType;

    public bool HasLineInfo { get; }

    public int LineNumber => HasLineInfo ? _nodeReaderLineInfo.LineNumber : 0;

    public int LinePosition => HasLineInfo ? _nodeReaderLineInfo.LinePosition : 0;

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                _innerReader.Close();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    private static XamlException CreateXamlException(string message, IXamlLineInfo lineInfo)
    {
        if (lineInfo != null && lineInfo.HasLineInfo)
        {
            return new XamlException(message, null, lineInfo.LineNumber, lineInfo.LinePosition);
        }

        return new XamlException(message);
    }

    // perf optimization to efficiently support non-Activity types
    private void DisableRewrite()
    {
        _notRewriting = true;
        _nodeReader = _innerReader;
        _nodeReaderLineInfo = _innerReader as IXamlLineInfo;
    }

    public override bool Read()
    {
        if (_notRewriting)
        {
            Fx.Assert(ReferenceEquals(_innerReader, _nodeReader), "readers must match at this point");
            return _nodeReader.Read();
        }

        // for properties, we'll store nodes "on the side"
        var innerReaderResult = _innerReader.Read();
        var continueProcessing = true;
        while (continueProcessing && !_innerReader.IsEof)
            // ProcessCurrentNode will only return true if it has advanced the innerReader
        {
            continueProcessing = ProcessCurrentNode();
        }

        // rewriting may have been disabled under ProcessCurrentNode
        if (_notRewriting)
        {
            return innerReaderResult;
        }

        return _nodeReader.Read();
    }

    // pull on our inner reader, map the results as necessary, and pump
    // mapped results into the streaming node reader that we're offering up.
    // return true if we need to keep pumping (because we've buffered some nodes on the side)
    private bool ProcessCurrentNode()
    {
        var processedNode = false;
        _namespaceTable.ManageNamespace(_innerReader);

        switch (_innerReader.NodeType)
        {
            case XamlNodeType.StartMember:
                var currentMember = _innerReader.Member;
                // find out if the member is a default value for one of
                // our declared properties. If it is, then we have a complex case
                // where we need to:
                // 1) read the nodes into a side list 
                // 2) interleave these nodes with the DynamicActivityProperty nodes
                //    since they need to appear as DynamicActivityProperty.Value
                // 3) right before we hit the last node, we'll dump the side node-lists
                //    reflecting a zipped up representation of the Properties
                if (IsXClassName(currentMember.DeclaringType))
                {
                    _bufferedProperties ??= new BufferedPropertyList(this);

                    _bufferedProperties.BufferDefaultValue(currentMember.Name, _activityPropertyValue, _innerReader,
                        _innerReaderLineInfo);
                    return true; // output cursor didn't move forward
                }
                else if (_frontLoadedDirectives && currentMember == XamlLanguage.FactoryMethod)
                {
                    DisableRewrite();
                    return false;
                }
                else
                {
                    _depth++;
                    if (_depth == 2)
                    {
                        if (currentMember.DeclaringType == _activityXamlType ||
                            currentMember.DeclaringType == _baseActivityXamlType)
                        {
                            // Rewrite "<Activity.XXX>" to "<DynamicActivity.XXX>"
                            var member = _activityReplacementXamlType.GetMember(currentMember.Name);
                            if (member == null)
                            {
                                throw FxTrace.Exception.AsError(CreateXamlException(
                                    SR.MemberNotSupportedByActivityXamlServices(currentMember.Name),
                                    _innerReaderLineInfo));
                            }

                            _nodeQueue.Writer.WriteStartMember(member, _innerReaderLineInfo);

                            if (member.Name == "Constraints")
                            {
                                WriteWrappedMember(true);
                                processedNode = true;
                                return true;
                            }

                            processedNode = true;

                            // if we're in ActivityBuilder.Implementation, start buffering nodes
                            if (_isBuilder && member.Name == "Implementation")
                            {
                                _builderStack = new BuilderStack(this);
                            }
                        }
                        else if (currentMember == XamlLanguage.Class)
                        {
                            _inXClassDepth = _depth;

                            // Rewrite x:Class to DynamicActivity.Name
                            _nodeQueue.Writer.WriteStartMember(_activityReplacementXamlType.GetMember("Name"),
                                _innerReaderLineInfo);
                            processedNode = true;
                        }
                        else if (currentMember == XamlLanguage.Members)
                        {
                            // Rewrite "<x:Members>" to "<DynamicActivity.Properties>"
                            _bufferedProperties ??= new BufferedPropertyList(this);

                            _bufferedProperties.BufferDefinitions(this);
                            _depth--;
                            return true; // output cursor didn't move forward
                        }
                        else if (currentMember == XamlLanguage.ClassAttributes)
                        {
                            // Rewrite x:ClassAttributes to DynamicActivity.Attributes
                            _nodeQueue.Writer.WriteStartMember(_activityReplacementXamlType.GetMember("Attributes"),
                                _innerReaderLineInfo);
                            // x:ClassAttributes directive has no following GetObject, but Attributes does since it's not a directive
                            WriteWrappedMember(false);
                            processedNode = true;
                            return true;
                        }
                    }
                }

                break;

            case XamlNodeType.StartObject:
                EnterObject();
                if (_depth == 1)
                {
                    // see if we're deserializing an Activity
                    if (_innerReader.Type.UnderlyingType == typeof(Activity))
                    {
                        // Rewrite "<Activity>" to "<DynamicActivity>"
                        _activityXamlType = _innerReader.Type;
                        _activityReplacementXamlType =
                            SchemaContext.GetXamlType(_isBuilder ? typeof(ActivityBuilder) : typeof(DynamicActivity));
                    }
                    // or an Activity<TResult>
                    else if (_innerReader.Type.IsGeneric && _innerReader.Type.UnderlyingType != null
                             && _innerReader.Type.UnderlyingType.GetGenericTypeDefinition() ==
                             typeof(Activity<>))
                    {
                        // Rewrite "<Activity typeArgument=T>" to "<DynamicActivity typeArgument=T>" 
                        _activityXamlType = _innerReader.Type;

                        var activityType = _innerReader.Type.TypeArguments[0].UnderlyingType;
                        var activityReplacementGenericType = _isBuilder
                            ? typeof(ActivityBuilder<>).MakeGenericType(activityType)
                            : typeof(DynamicActivity<>).MakeGenericType(activityType);

                        _activityReplacementXamlType = SchemaContext.GetXamlType(activityReplacementGenericType);
                    }
                    // otherwise disable rewriting so that we're a pass through
                    else
                    {
                        DisableRewrite();
                        return false;
                    }

                    _nodeQueue.Writer.WriteStartObject(_activityReplacementXamlType, _innerReaderLineInfo);
                    processedNode = true;
                }

                break;

            case XamlNodeType.GetObject:
                EnterObject();
                break;

            case XamlNodeType.EndObject:
            case XamlNodeType.EndMember:
                ExitObject();
                break;

            case XamlNodeType.Value:
                if (_inXClassDepth >= _depth && _xClassName == null)
                {
                    var fullName = (string) _innerReader.Value;
                    var xClassNamespace = "";
                    var xClassName = fullName;

                    var nameStartIndex = fullName.LastIndexOf('.');
                    if (nameStartIndex > 0)
                    {
                        xClassNamespace = fullName[..nameStartIndex];
                        xClassName = fullName[(nameStartIndex + 1)..];
                    }

                    _xClassName = new XamlTypeName(xClassNamespace, xClassName);
                }

                break;
        }

        if (!processedNode)
        {
            if (_builderStack != null)
            {
                _builderStack.ProcessNode(_innerReader, _innerReaderLineInfo, _nodeQueue.Writer, out var writeNode);
                if (!writeNode)
                {
                    _innerReader.Read();
                    return true;
                }
            }

            _nodeQueue.Writer.WriteNode(_innerReader, _innerReaderLineInfo);
        }

        return false;
    }

    // used for a number of cases when wrapping we need to add a GetObject/StartMember(_Items) since XAML directives intrinsically
    // take care of it
    private void WriteWrappedMember(bool stripWhitespace)
    {
        _nodeQueue.Writer.WriteGetObject(_innerReaderLineInfo);
        _nodeQueue.Writer.WriteStartMember(XamlLanguage.Items, _innerReaderLineInfo);
        var subReader = _innerReader.ReadSubtree();

        // 1) Read past the start member since we wrote it above
        subReader.Read();

        // 2) copy over the rest of the subnodes, possibly discarding top-level whitespace from WhitespaceSignificantCollection
        subReader.Read();
        while (!subReader.IsEof)
        {
            var isWhitespaceNode = false;
            if (subReader.NodeType == XamlNodeType.Value)
            {
                if (subReader.Value is string stringValue && stringValue.Trim().Length == 0)
                {
                    isWhitespaceNode = true;
                }
            }

            if (isWhitespaceNode && stripWhitespace)
            {
                subReader.Read();
            }
            else
            {
                XamlWriterExtensions.Transform(subReader.ReadSubtree(), _nodeQueue.Writer, _innerReaderLineInfo, false);
            }
        }


        // close the GetObject added above. Note that we are doing EndObject/EndMember after the last node (EndMember) 
        // rather than inserting EndMember/EndObject before the last EndMember since all EndMembers are interchangable from a state perspective
        _nodeQueue.Writer.WriteEndObject(_innerReaderLineInfo);
        _nodeQueue.Writer.WriteEndMember(_innerReaderLineInfo);

        subReader.Close();

        // we hand exited a member where we had increased the depth manually, so record that fact
        ExitObject();
    }

    // when Read hits StartObject or GetObject
    private void EnterObject()
    {
        _depth++;
        if (_depth >= 2)
        {
            _frontLoadedDirectives = false;
        }
    }

    // when Read hits EndObject or EndMember
    private void ExitObject()
    {
        if (_depth <= _inXClassDepth)
        {
            _inXClassDepth = 0;
        }

        _depth--;
        _frontLoadedDirectives = false;

        if (_depth == 1)
        {
            _builderStack = null;
        }
        else if (_depth == 0)
        {
            // we're about to write out the last tag. Dump our accrued properties 
            // as no more property values are forthcoming.
            _bufferedProperties?.FlushTo(_nodeQueue, this);
        }
    }

    private bool IsXClassName(XamlType xamlType)
    {
        if (xamlType == null || _xClassName == null || xamlType.Name != _xClassName.Name)
        {
            return false;
        }

        // this code is kept for back compatible
        var preferredNamespace = xamlType.PreferredXamlNamespace;
        if (preferredNamespace.Contains(ClrNamespacePart))
        {
            return IsXClassName(preferredNamespace);
        }

        // GetXamlNamespaces is a superset of PreferredXamlNamespace, it's not a must for the above code
        // to check for preferredXamlNamespace, but since the old code uses .Contains(), which was a minor bug,
        // we decide to use StartsWith in new code and keep the old code for back compatible reason.
        var namespaces = xamlType.GetXamlNamespaces();
        return (namespaces.Where(ns => ns.StartsWith(ClrNamespacePart, StringComparison.Ordinal))
                          .Select(IsXClassName)).FirstOrDefault();
    }

    private bool IsXClassName(string ns)
    {
        var clrNamespace = ns[ClrNamespacePart.Length..];

        var lastIndex = clrNamespace.IndexOf(';');
        if (lastIndex < 0 || lastIndex > clrNamespace.Length)
        {
            lastIndex = clrNamespace.Length;
        }

        var @namespace = clrNamespace.Substring(0, lastIndex);
        return _xClassName.Namespace == @namespace;
    }

    private static void IncrementIfPositive(ref int a)
    {
        if (a > 0)
        {
            a++;
        }
    }

    private static void DecrementIfPositive(ref int a)
    {
        if (a > 0)
        {
            a--;
        }
    }

    // This class tracks the information we need to be able to convert
    // <PropertyReferenceExtension> into <ActivityBuilder.PropertyReferences>
    private class BuilderStack
    {
        private readonly XamlMember _activityBuilderPropertyReferencesMember;
        private readonly XamlMember _activityPropertyReferenceSourceProperty;
        private readonly XamlMember _activityPropertyReferenceTargetProperty;
        private readonly XamlType _activityPropertyReferenceXamlType;
        private readonly DynamicActivityXamlReader _parent;
        private readonly Stack<Frame> _stack;
        private MemberInformation _bufferedMember;

        public BuilderStack(DynamicActivityXamlReader parent)
        {
            _parent = parent;
            _stack = new Stack<Frame>();
            _activityPropertyReferenceXamlType = parent._schemaContext.GetXamlType(typeof(ActivityPropertyReference));
            _activityPropertyReferenceSourceProperty = _activityPropertyReferenceXamlType.GetMember("SourceProperty");
            _activityPropertyReferenceTargetProperty = _activityPropertyReferenceXamlType.GetMember("TargetProperty");
            var typeOfActivityBuilder = parent._schemaContext.GetXamlType(typeof(ActivityBuilder));
            _activityBuilderPropertyReferencesMember = typeOfActivityBuilder.GetAttachableMember("PropertyReferences");
        }

        private string ReadPropertyReferenceExtensionPropertyName(XamlReader reader)
        {
            string sourceProperty = null;
            reader.Read();
            while (!reader.IsEof && reader.NodeType != XamlNodeType.EndObject)
            {
                if (IsExpectedPropertyReferenceMember(reader))
                {
                    var propertyName = ReadPropertyName(reader);
                    if (propertyName != null)
                    {
                        sourceProperty = propertyName;
                    }
                }
                else
                {
                    // unexpected members.
                    // For compat with 4.0, unexpected members on PropertyReferenceExtension
                    // are silently ignored
                    reader.Skip();
                }
            }

            return sourceProperty;
        }

        // Whenever we encounter a StartMember, we buffer it (and any namespace nodes folllowing it)
        // until we see its contents (SO/GO/V).
        // If the content is a PropertyReferenceExtension, then we convert it to an ActivityPropertyReference
        // in the parent object's ActivityBuilder.PropertyReference collection, and dont' write out the member.
        // If the content is not a PropertyReferenceExtension, or there's no content (i.e. we hit an EM),
        // we flush the buffered SM + NS*, and continue as normal.
        public void ProcessNode(XamlReader reader, IXamlLineInfo lineInfo, XamlWriter targetWriter,
            out bool writeNodeToOutput)
        {
            writeNodeToOutput = true;

            switch (reader.NodeType)
            {
                case XamlNodeType.StartMember:
                    _bufferedMember = new MemberInformation(reader.Member, lineInfo);
                    writeNodeToOutput = false;
                    break;

                case XamlNodeType.EndMember:
                    FlushBufferedMember(targetWriter);
                    if (_stack.Count > 0)
                    {
                        var curFrame = _stack.Peek();
                        if (curFrame.SuppressNextEndMember)
                        {
                            writeNodeToOutput = false;
                            curFrame.SuppressNextEndMember = false;
                        }
                    }

                    break;

                case XamlNodeType.StartObject:
                    Frame newFrame;
                    if (IsPropertyReferenceExtension(reader.Type) && _bufferedMember.IsSet)
                    {
                        var targetMember = _bufferedMember;
                        _bufferedMember = MemberInformation.None;
                        WritePropertyReferenceFrameToParent(targetMember,
                            ReadPropertyReferenceExtensionPropertyName(reader), _stack.Peek(), lineInfo);
                        writeNodeToOutput = false;
                        break;
                    }
                    else
                    {
                        FlushBufferedMember(targetWriter);
                        newFrame = new Frame();
                    }

                    _stack.Push(newFrame);
                    break;

                case XamlNodeType.GetObject:
                    FlushBufferedMember(targetWriter);
                    _stack.Push(new Frame());
                    break;

                case XamlNodeType.EndObject:
                    var frame = _stack.Pop();
                    if (frame.PropertyReferences != null)
                    {
                        WritePropertyReferenceCollection(frame.PropertyReferences, targetWriter, lineInfo);
                    }

                    break;

                case XamlNodeType.Value:
                    FlushBufferedMember(targetWriter);
                    break;

                case XamlNodeType.NamespaceDeclaration:
                    if (_bufferedMember.IsSet)
                    {
                        _bufferedMember.FollowingNamespaces ??= new XamlNodeQueue(_parent._schemaContext);
                        _bufferedMember.FollowingNamespaces.Writer.WriteNode(reader, lineInfo);
                        writeNodeToOutput = false;
                    }

                    break;
            }
        }

        private void FlushBufferedMember(XamlWriter targetWriter)
        {
            if (_bufferedMember.IsSet)
            {
                _bufferedMember.Flush(targetWriter);
                _bufferedMember = MemberInformation.None;
            }
        }

        private static bool IsPropertyReferenceExtension(XamlType type)
        {
            return type != null && type.IsGeneric && type.UnderlyingType != null &&
                type.Name == "PropertyReferenceExtension"
                && type.UnderlyingType.GetGenericTypeDefinition() == typeof(PropertyReferenceExtension<>);
        }

        private static bool IsExpectedPropertyReferenceMember(XamlReader reader)
        {
            return reader.NodeType == XamlNodeType.StartMember &&
                IsPropertyReferenceExtension(reader.Member.DeclaringType) && reader.Member.Name == "PropertyName";
        }

        private string ReadPropertyName(XamlReader reader)
        {
            Fx.Assert(reader.Member.Name == "PropertyName", "Exepcted PropertyName member");
            string result = null;
            while (reader.Read() && reader.NodeType != XamlNodeType.EndMember)
            {
                // For compat with 4.0, we only need to support PropertyName as Value node
                if (reader.NodeType == XamlNodeType.Value && reader.Value is string propertyName)
                {
                    result = propertyName;
                }
            }

            if (reader.NodeType == XamlNodeType.EndMember)
                // Our parent will never see this EndMember node so we need to force its
                // depth count to decrement
            {
                _parent.ExitObject();
            }

            return result;
        }

        private void WritePropertyReferenceCollection(XamlNodeQueue serializedReferences, XamlWriter targetWriter,
            IXamlLineInfo lineInfo)
        {
            targetWriter.WriteStartMember(_activityBuilderPropertyReferencesMember, lineInfo);
            targetWriter.WriteGetObject(lineInfo);
            targetWriter.WriteStartMember(XamlLanguage.Items, lineInfo);
            XamlServices.Transform(serializedReferences.Reader, targetWriter, false);
            targetWriter.WriteEndMember(lineInfo);
            targetWriter.WriteEndObject(lineInfo);
            targetWriter.WriteEndMember(lineInfo);
        }

        private void WritePropertyReferenceFrameToParent(MemberInformation targetMember, string sourceProperty,
            Frame parentFrame, IXamlLineInfo lineInfo)
        {
            parentFrame.PropertyReferences ??= new XamlNodeQueue(_parent._schemaContext);

            WriteSerializedPropertyReference(parentFrame.PropertyReferences.Writer, lineInfo, targetMember.Member.Name,
                sourceProperty);

            // we didn't write out the target
            // StartMember, so suppress the EndMember
            parentFrame.SuppressNextEndMember = true;
        }

        private void WriteSerializedPropertyReference(XamlWriter targetWriter, IXamlLineInfo lineInfo,
            string targetName, string sourceName)
        {
            // Line Info for the entire <ActivityPropertyReference> element 
            // comes from the end of the <PropertyReference> tag
            targetWriter.WriteStartObject(_activityPropertyReferenceXamlType, lineInfo);
            targetWriter.WriteStartMember(_activityPropertyReferenceTargetProperty, lineInfo);
            targetWriter.WriteValue(targetName, lineInfo);
            targetWriter.WriteEndMember(lineInfo);
            if (sourceName != null)
            {
                targetWriter.WriteStartMember(_activityPropertyReferenceSourceProperty, lineInfo);
                targetWriter.WriteValue(sourceName, lineInfo);
                targetWriter.WriteEndMember(lineInfo);
            }

            targetWriter.WriteEndObject(lineInfo);
        }

        private struct MemberInformation
        {
            public static readonly MemberInformation None = new();

            public XamlMember Member { get; }
            public int LineNumber { get; }
            public int LinePosition { get; }
            public XamlNodeQueue FollowingNamespaces { get; set; }

            public MemberInformation(XamlMember member, IXamlLineInfo lineInfo)
                : this()
            {
                Member = member;
                if (lineInfo != null)
                {
                    LineNumber = lineInfo.LineNumber;
                    LinePosition = lineInfo.LinePosition;
                }
            }

            public bool IsSet => Member != null;

            public void Flush(XamlWriter targetWriter)
            {
                targetWriter.WriteStartMember(Member, LineNumber, LinePosition);
                if (FollowingNamespaces != null)
                {
                    XamlServices.Transform(FollowingNamespaces.Reader, targetWriter, false);
                }
            }
        }

        private class Frame
        {
            public XamlNodeQueue PropertyReferences { get; set; }
            public bool SuppressNextEndMember { get; set; }
        }
    }

    // This class exists to "zip" together <x:Member> property definitions (to be rewritten as <DynamicActivityProperty> nodes)
    // with their corresponding default values <MyClass.Foo> (to be rewritten as <DynamicActivityProperty.Value> nodes).
    // Definitions come all at once, but values could come anywhere in the XAML document, so we save them all almost until the end of
    // the document and write them all out at once using BufferedPropertyList.CopyTo().
    private class BufferedPropertyList
    {
        private readonly XamlNodeQueue _outerNodes;
        private readonly DynamicActivityXamlReader _parent;
        private bool _alreadyBufferedDefinitions;
        private Dictionary<string, ActivityPropertyHolder> _propertyHolders;
        private Dictionary<string, ValueHolder> _valueHolders;

        public BufferedPropertyList(DynamicActivityXamlReader parent)
        {
            _parent = parent;
            _outerNodes = new XamlNodeQueue(parent.SchemaContext);
        }

        private Dictionary<string, ActivityPropertyHolder> PropertyHolders =>
            _propertyHolders ??= new Dictionary<string, ActivityPropertyHolder>();

        // Called inside of an x:Members--read up to </x:Members>, buffering definitions
        public void BufferDefinitions(DynamicActivityXamlReader parent)
        {
            var subReader = parent._innerReader.ReadSubtree();
            var readerLineInfo = parent._innerReaderLineInfo;

            // 1) swap out the start member with <DynamicActivity.Properties>
            subReader.Read();
            Fx.Assert(subReader.NodeType == XamlNodeType.StartMember && subReader.Member == XamlLanguage.Members,
                "Should be inside of x:Members before calling BufferDefinitions");
            _outerNodes.Writer.WriteStartMember(parent._activityReplacementXamlType.GetMember("Properties"),
                readerLineInfo);

            // x:Members directive has no following GetObject, but Properties does since it's not a directive
            _outerNodes.Writer.WriteGetObject(readerLineInfo);
            _outerNodes.Writer.WriteStartMember(XamlLanguage.Items, readerLineInfo);

            // 2) process the subnodes and store them in either ActivityPropertyHolders,
            // or exigent nodes in the outer node list
            var continueReading = subReader.Read();
            while (continueReading)
            {
                if (subReader.NodeType == XamlNodeType.StartObject
                    && subReader.Type == XamlLanguage.Property)
                {
                    // we found an x:Property. Store it in an ActivityPropertyHolder
                    var newProperty = new ActivityPropertyHolder(parent, subReader.ReadSubtree());
                    PropertyHolders.Add(newProperty.Name, newProperty);

                    // and stash away a proxy node to map later
                    _outerNodes.Writer.WriteValue(newProperty, readerLineInfo);

                    // ActivityPropertyHolder consumed the subtree, so we don't need to pump a Read() in this path
                }
                else
                {
                    // it's not an x:Property. Store it in our extra node list
                    _outerNodes.Writer.WriteNode(subReader, readerLineInfo);
                    continueReading = subReader.Read();
                }
            }

            // close the GetObject added above. Note that we are doing EndObject/EndMember after the last node (EndMember) 
            // rather than inserting EndMember/EndObject before the last EndMember since all EndMembers are interchangable from a state perspective
            _outerNodes.Writer.WriteEndObject(readerLineInfo);
            _outerNodes.Writer.WriteEndMember(readerLineInfo);
            subReader.Close();

            _alreadyBufferedDefinitions = true;
            FlushValueHolders();
        }

        private void FlushValueHolders()
        {
            // We've seen all the property definitions we're going to see. Write out any values already accumulated.

            // If we have picked up any values already before definitions, process them immediately 
            // (and throw as usual if corresponding definition doesn't exist)
            if (_valueHolders == null)
            {
                return;
            }

            foreach (var (key, value) in _valueHolders)
            {
                ProcessDefaultValue(key, value.PropertyValue,
                    value.ValueReader,
                    value.ValueReader as IXamlLineInfo);
            }

            _valueHolders = null; // So we don't flush it again at close
        }

        public void BufferDefaultValue(string propertyName, XamlMember propertyValue, XamlReader reader,
            IXamlLineInfo lineInfo)
        {
            if (_alreadyBufferedDefinitions)
            {
                ProcessDefaultValue(propertyName, propertyValue, reader.ReadSubtree(), lineInfo);
            }
            else
            {
                _valueHolders ??= new Dictionary<string, ValueHolder>();
                var savedValue = new ValueHolder(_parent.SchemaContext, propertyValue, reader, lineInfo);
                _valueHolders[propertyName] = savedValue;
            }
        }

        public void ProcessDefaultValue(string propertyName, XamlMember propertyValue, XamlReader reader,
            IXamlLineInfo lineInfo)
        {
            if (!PropertyHolders.TryGetValue(propertyName, out var propertyHolder))
            {
                throw FxTrace.Exception.AsError(CreateXamlException(SR.InvalidProperty(propertyName), lineInfo));
            }

            propertyHolder.ProcessDefaultValue(propertyValue, reader, lineInfo);
        }

        public void FlushTo(XamlNodeQueue targetNodeQueue, DynamicActivityXamlReader parent)
        {
            FlushValueHolders();

            var sourceReader = _outerNodes.Reader;
            var sourceReaderLineInfo = parent.HasLineInfo ? sourceReader as IXamlLineInfo : null;
            while (sourceReader.Read())
            {
                if (sourceReader.NodeType == XamlNodeType.Value &&
                    sourceReader.Value is ActivityPropertyHolder propertyHolder)
                {
                    // replace ActivityPropertyHolder with its constituent nodes
                    propertyHolder.CopyTo(targetNodeQueue, sourceReaderLineInfo);
                    continue;
                }

                targetNodeQueue.Writer.WriteNode(sourceReader, sourceReaderLineInfo);
            }
        }

        // Buffer property values until we can match them with definitions
        private class ValueHolder
        {
            private readonly XamlNodeQueue _nodes;

            public ValueHolder(XamlSchemaContext schemaContext, XamlMember propertyValue, XamlReader reader,
                IXamlLineInfo lineInfo)
            {
                _nodes = new XamlNodeQueue(schemaContext);
                PropertyValue = propertyValue;
                XamlWriterExtensions.Transform(reader.ReadSubtree(), _nodes.Writer, lineInfo, true);
            }

            public XamlMember PropertyValue { get; }

            public XamlReader ValueReader => _nodes.Reader;
        }

        private class ActivityPropertyHolder
        {
            // the nodes that we'll pump at the end
            private readonly XamlNodeQueue _nodes;
            private readonly DynamicActivityXamlReader _parent;

            public ActivityPropertyHolder(DynamicActivityXamlReader parent, XamlReader reader)
            {
                _parent = parent;
                _nodes = new XamlNodeQueue(parent.SchemaContext);
                var readerLineInfo = parent._innerReaderLineInfo;

                // parse the subtree, and extract out the Name and Type for now.
                // keep the node-list open for now, just in case a default value appears 
                // later in the document

                // Rewrite "<x:Property>" to "<DynamicActivityProperty>"
                reader.Read();
                _nodes.Writer.WriteStartObject(parent._activityPropertyXamlType, readerLineInfo);
                var depth = 1;
                var nameDepth = 0;
                var typeDepth = 0;
                var continueReading = reader.Read();
                while (continueReading)
                {
                    switch (reader.NodeType)
                    {
                        case XamlNodeType.StartMember:
                            // map <x:Property> members to the appropriate <DynamicActivity.Property> members
                            if (reader.Member.DeclaringType == XamlLanguage.Property)
                            {
                                var mappedMember = reader.Member;

                                if (mappedMember == XPropertyName)
                                {
                                    mappedMember = parent._activityPropertyName;
                                    if (nameDepth == 0)
                                    {
                                        nameDepth = 1;
                                    }
                                }
                                else if (mappedMember == XPropertyType)
                                {
                                    mappedMember = parent._activityPropertyType;
                                    if (typeDepth == 0)
                                    {
                                        typeDepth = 1;
                                    }
                                }
                                else if (mappedMember == XPropertyAttributes)
                                {
                                    mappedMember = parent._activityPropertyAttributes;
                                }
                                else
                                {
                                    throw FxTrace.Exception.AsError(CreateXamlException(
                                        SR.PropertyMemberNotSupportedByActivityXamlServices(mappedMember.Name),
                                        readerLineInfo));
                                }

                                _nodes.Writer.WriteStartMember(mappedMember, readerLineInfo);
                                continueReading = reader.Read();
                                continue;
                            }

                            break;

                        case XamlNodeType.Value:
                            if (nameDepth == 1)
                            {
                                // We only support property name as an attribute (nameDepth == 1)
                                Name = reader.Value as string;
                            }
                            else if (typeDepth == 1)
                            {
                                // We only support property type as an attribute (typeDepth == 1)
                                var xamlTypeName = XamlTypeName.Parse(reader.Value as string, parent._namespaceTable);
                                var xamlType = parent.SchemaContext.GetXamlType(xamlTypeName);
                                Type = xamlType ?? throw FxTrace.Exception.AsError(
                                    CreateXamlException(SR.InvalidPropertyType(reader.Value as string, Name),
                                        readerLineInfo));
                            }

                            break;

                        case XamlNodeType.StartObject:
                        case XamlNodeType.GetObject:
                            depth++;
                            IncrementIfPositive(ref nameDepth);
                            IncrementIfPositive(ref typeDepth);
                            if (typeDepth > 0 && reader.Type == parent._xamlTypeXamlType)
                            {
                                _nodes.Writer.WriteStartObject(parent._typeXamlType, readerLineInfo);
                                continueReading = reader.Read();
                                continue;
                            }

                            break;

                        case XamlNodeType.EndObject:
                            depth--;
                            if (depth == 0)
                            {
                                continueReading = reader.Read();
                                continue; // skip this node, we'll close it by hand in CopyTo()
                            }

                            DecrementIfPositive(ref nameDepth);
                            DecrementIfPositive(ref typeDepth);
                            break;

                        case XamlNodeType.EndMember:
                            DecrementIfPositive(ref nameDepth);
                            DecrementIfPositive(ref typeDepth);
                            break;
                    }

                    // if we didn't continue (from a mapped case), just copy over
                    _nodes.Writer.WriteNode(reader, readerLineInfo);
                    continueReading = reader.Read();
                }

                reader.Close();
            }

            public string Name { get; }

            public XamlType Type { get; }

            // called when we've reached the end of the activity and need
            // to extract out the resulting data into our activity-wide node list
            public void CopyTo(XamlNodeQueue targetNodeQueue, IXamlLineInfo readerInfo)
            {
                // first copy any buffered nodes
                XamlServices.Transform(_nodes.Reader, targetNodeQueue.Writer, false);

                // then write the end node for this property
                targetNodeQueue.Writer.WriteEndObject(readerInfo);
            }

            public void ProcessDefaultValue(XamlMember propertyValue, XamlReader subReader, IXamlLineInfo lineInfo)
            {
                var addedStartObject = false;

                // 1) swap out the start member with <ActivityProperty.Value>
                subReader.Read();
                if (!subReader.Member.IsNameValid)
                {
                    throw FxTrace.Exception.AsError(CreateXamlException(SR.InvalidXamlMember(subReader.Member.Name),
                        lineInfo));
                }

                _nodes.Writer.WriteStartMember(propertyValue, lineInfo);

                // temporary hack: read past GetObject/StartMember nodes that are added by 
                // the XAML stack. This has been fixed in the WPF branch, but we haven't FI'ed that yet
                XamlReader valueReader;
                subReader.Read();
                if (subReader.NodeType == XamlNodeType.GetObject)
                {
                    subReader.Read();
                    subReader.Read();
                    valueReader = subReader.ReadSubtree();
                    valueReader.Read();
                }
                else
                {
                    valueReader = subReader;
                }

                // Add SO tag if necessary UNLESS there's no value to wrap (which means we're already at EO)
                if (valueReader.NodeType != XamlNodeType.EndMember && valueReader.NodeType != XamlNodeType.StartObject)
                {
                    addedStartObject = true;
                    // Add <TypeOfProperty> nodes so that type converters work correctly
                    _nodes.Writer.WriteStartObject(Type, lineInfo);
                    _nodes.Writer.WriteStartMember(XamlLanguage.Initialization, lineInfo);
                }

                // 3) copy over the value 
                while (!valueReader.IsEof)
                {
                    _nodes.Writer.WriteNode(valueReader, lineInfo);
                    valueReader.Read();
                }

                valueReader.Close();

                // 4) close up the extra nodes 
                if (!ReferenceEquals(valueReader, subReader))
                {
                    subReader.Read();
                    while (subReader.Read())
                    {
                        _nodes.Writer.WriteNode(subReader, lineInfo);
                    }
                }

                if (addedStartObject)
                {
                    _nodes.Writer.WriteEndObject(lineInfo);
                    _nodes.Writer.WriteEndMember(lineInfo);
                }

                subReader.Close();
            }
        }
    }
}
