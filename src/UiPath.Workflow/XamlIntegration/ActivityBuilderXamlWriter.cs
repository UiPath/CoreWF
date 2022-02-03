// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Globalization;
using System.Xaml;
using System.Xaml.Schema;

namespace System.Activities.XamlIntegration;

// This class rewrites an <ActivityBuilder to <Activity x:Class
// ActivityBuilder.Properties is rewritten to x:Members
// ActivityBuilder.Name is rewritten as x:Class
// ActivityBuilder.Implementation is rewritten as Activity.Implementation
// 
// Because of our [DependsOn] annotations, Name is followed by Attributes, Properties,
// Constraints and, lastly, Implementation. The first few relationships are assumed
// and enforced through our state machine here to avoid buffering the whole node stream
// in common cases (such as no attributes specified).
internal class ActivityBuilderXamlWriter : XamlWriter
{
    private readonly XamlWriter _innerWriter;

    // we need to accrue namespace so that we can resolve DynamicActivityProperty.Type
    // and correctly strip superfluous wrapper nodes around default values
    private readonly NamespaceTable _namespaceTable;
    private XamlMember _activityBuilderAttributes;
    private XamlMember _activityBuilderName;
    private XamlMember _activityBuilderProperties;
    private XamlMember _activityBuilderPropertyReference;
    private XamlMember _activityBuilderPropertyReferences;

    // These may be a closed generic type in the Activity<T> case (or null if not an ActivityBuilder),
    // so we need to compute this value dynamically
    private XamlType _activityBuilderXamlType;
    private XamlMember _activityPropertyName;
    private XamlType _activityPropertyReferenceXamlType;
    private XamlMember _activityPropertyType;
    private XamlMember _activityPropertyValue;
    private XamlType _activityPropertyXamlType;
    private XamlType _activityXamlType;
    private int _currentDepth;
    private BuilderXamlNode _currentState;
    private bool _notRewriting;
    private Stack<BuilderXamlNode> _pendingStates;
    private XamlType _typeXamlType;
    private XamlType _xamlTypeXamlType;

    public ActivityBuilderXamlWriter(XamlWriter innerWriter)
    {
        _innerWriter = innerWriter;
        _currentState = new RootNode(this);
        _namespaceTable = new NamespaceTable();
    }

    public override XamlSchemaContext SchemaContext => _innerWriter.SchemaContext;

    private void SetActivityType(XamlType activityXamlType, XamlType activityBuilderXamlType)
    {
        if (activityXamlType == null)
        {
            _notRewriting = true;
        }
        else
        {
            _activityXamlType = activityXamlType;
            _activityBuilderXamlType = activityBuilderXamlType;
            _xamlTypeXamlType = SchemaContext.GetXamlType(typeof(XamlType));
            _typeXamlType = SchemaContext.GetXamlType(typeof(Type));

            _activityPropertyXamlType = SchemaContext.GetXamlType(typeof(DynamicActivityProperty));
            _activityPropertyType = _activityPropertyXamlType.GetMember("Type");
            _activityPropertyName = _activityPropertyXamlType.GetMember("Name");
            _activityPropertyValue = _activityPropertyXamlType.GetMember("Value");

            _activityBuilderName = _activityBuilderXamlType.GetMember("Name");
            _activityBuilderAttributes = _activityBuilderXamlType.GetMember("Attributes");
            _activityBuilderProperties = _activityBuilderXamlType.GetMember("Properties");
            _activityBuilderPropertyReference = SchemaContext.GetXamlType(typeof(ActivityBuilder))
                                                            .GetAttachableMember("PropertyReference");
            _activityBuilderPropertyReferences = SchemaContext.GetXamlType(typeof(ActivityBuilder))
                                                             .GetAttachableMember("PropertyReferences");
            _activityPropertyReferenceXamlType = SchemaContext.GetXamlType(typeof(ActivityPropertyReference));
        }
    }

    public override void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
    {
        if (_notRewriting)
        {
            _innerWriter.WriteNamespace(namespaceDeclaration);
            return;
        }

        _namespaceTable?.AddNamespace(namespaceDeclaration);

        _currentState.WriteNamespace(namespaceDeclaration);
    }

    public override void WriteValue(object value)
    {
        if (_notRewriting)
        {
            _innerWriter.WriteValue(value);
            return;
        }

        _currentState.WriteValue(value);
    }

    public override void WriteStartObject(XamlType xamlType)
    {
        if (_notRewriting)
        {
            _innerWriter.WriteStartObject(xamlType);
            return;
        }

        EnterDepth();
        _currentState.WriteStartObject(xamlType);
    }

    public override void WriteGetObject()
    {
        if (_notRewriting)
        {
            _innerWriter.WriteGetObject();
            return;
        }

        EnterDepth();
        _currentState.WriteGetObject();
    }

    public override void WriteEndObject()
    {
        if (_notRewriting)
        {
            _innerWriter.WriteEndObject();
            return;
        }

        _currentState.WriteEndObject();
        ExitDepth();
    }

    public override void WriteStartMember(XamlMember xamlMember)
    {
        if (_notRewriting)
        {
            _innerWriter.WriteStartMember(xamlMember);
            return;
        }

        EnterDepth();
        _currentState.WriteStartMember(xamlMember);
    }

    public override void WriteEndMember()
    {
        if (_notRewriting)
        {
            _innerWriter.WriteEndMember();
            return;
        }

        _currentState.WriteEndMember();
        ExitDepth();
    }

    private void PushState(BuilderXamlNode state)
    {
        if (_pendingStates == null)
        {
            _pendingStates = new Stack<BuilderXamlNode>();
        }

        _pendingStates.Push(_currentState);
        _currentState = state;
    }

    private void EnterDepth()
    {
        Fx.Assert(!_notRewriting, "we only use depth calculation if we're rewriting");
        _currentDepth++;
        _namespaceTable?.EnterScope();
    }

    private void ExitDepth()
    {
        Fx.Assert(!_notRewriting, "we only use depth calculation if we're rewriting");
        if (_currentState.Depth == _currentDepth)
        {
            // complete the current state
            _currentState.Complete();

            // and pop off the next state to look for
            if (_pendingStates.Count > 0)
            {
                _currentState = _pendingStates.Pop();
            }
        }

        _currentDepth--;
        _namespaceTable?.ExitScope();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ((IDisposable) _innerWriter).Dispose();
        }
    }

    private abstract class BuilderXamlNode
    {
        protected BuilderXamlNode(ActivityBuilderXamlWriter writer)
        {
            Depth = writer._currentDepth;
            Writer = writer;
            CurrentWriter = writer._innerWriter;
        }

        public int Depth { get; }

        // a lot of nodes just redirect output, this
        // allows them to avoid overriding everything just for that
        public XamlWriter CurrentWriter { get; protected set; }

        protected ActivityBuilderXamlWriter Writer { get; }

        protected internal virtual void Complete() { }

        protected internal virtual void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
        {
            CurrentWriter.WriteNamespace(namespaceDeclaration);
        }

        protected internal virtual void WriteStartObject(XamlType xamlType)
        {
            CurrentWriter.WriteStartObject(xamlType);
        }

        protected internal virtual void WriteGetObject()
        {
            CurrentWriter.WriteGetObject();
        }

        protected internal virtual void WriteEndObject()
        {
            CurrentWriter.WriteEndObject();
        }

        protected internal virtual void WriteStartMember(XamlMember xamlMember)
        {
            CurrentWriter.WriteStartMember(xamlMember);
        }

        protected internal virtual void WriteEndMember()
        {
            CurrentWriter.WriteEndMember();
        }

        protected internal virtual void WriteValue(object value)
        {
            CurrentWriter.WriteValue(value);
        }
    }

    // RootNode needs to buffer nodes until we finish processing Name + Properties
    // because we need to insert our namespace _before_ the first StartObject.
    // this is the starting value for ActivityBuilderXamlWriter.currentNode
    private class RootNode : BuilderXamlNode
    {
        private const string PreferredXamlNamespaceAlias = "x";
        private const string PreferredClassAlias = "this";
        private XamlNodeQueue _pendingNodes;
        private HashSet<string> _rootLevelPrefixes;
        private bool _wroteXamlNamespace;

        public RootNode(ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _pendingNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _pendingNodes.Writer;
        }

        protected internal override void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
        {
            if (Writer._currentDepth == 0 && !_wroteXamlNamespace)
            {
                if (namespaceDeclaration.Namespace == XamlLanguage.Xaml2006Namespace)
                {
                    _wroteXamlNamespace = true;
                }
                else
                {
                    if (_rootLevelPrefixes == null)
                    {
                        _rootLevelPrefixes = new HashSet<string>();
                    }

                    _rootLevelPrefixes.Add(namespaceDeclaration.Prefix);
                }
            }

            base.WriteNamespace(namespaceDeclaration);
        }

        protected internal override void WriteStartObject(XamlType xamlType)
        {
            if (Writer._currentDepth == 1)
            {
                XamlType activityXamlType = null;

                // root object: see if we're serializing an ActivityBuilder
                if (xamlType.UnderlyingType == typeof(ActivityBuilder))
                {
                    activityXamlType = Writer.SchemaContext.GetXamlType(typeof(Activity));
                }
                // or an ActivityBuilder<TResult>
                else if (xamlType.IsGeneric && xamlType.UnderlyingType != null
                         && xamlType.UnderlyingType.GetGenericTypeDefinition() ==
                         typeof(ActivityBuilder<>))
                {
                    var activityType = xamlType.TypeArguments[0].UnderlyingType;
                    activityXamlType =
                        Writer.SchemaContext.GetXamlType(typeof(Activity<>).MakeGenericType(activityType));
                }

                Writer.SetActivityType(activityXamlType, xamlType);

                if (activityXamlType != null)
                {
                    Writer.PushState(new BuilderClassNode(this, Writer));
                    return;
                }

                // we should be a pass through. Flush any buffered nodes and get out of the way
                FlushPendingNodes(null);
            }

            base.WriteStartObject(xamlType);
        }

        public void FlushPendingNodes(string classNamespace)
        {
            CurrentWriter = Writer._innerWriter;
            if (!Writer._notRewriting)
            {
                // make sure we have any required namespaces
                if (!_wroteXamlNamespace)
                {
                    var xamlNamespaceAlias = GenerateNamespacePrefix(PreferredXamlNamespaceAlias);
                    WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, xamlNamespaceAlias));
                }

                // If there's an x:Class="Foo.Bar", add a namespace declaration for Foo in the local assembly so we can 
                // say stuff like this:Bar.MyProperty later on. DON'T add the namespace declaration if somebody has already 
                // declared the namespace in the nodestream though (duplicates are an error).
                if (classNamespace != null)
                {
                    var sawClassNamespace = false;

                    var reader = _pendingNodes.Reader;
                    var writer = Writer._innerWriter;
                    while (reader.Read() && reader.NodeType == XamlNodeType.NamespaceDeclaration)
                    {
                        if (classNamespace.Equals(reader.Namespace.Namespace))
                        {
                            sawClassNamespace = true;
                        }

                        writer.WriteNode(reader);
                    }

                    if (!sawClassNamespace)
                    {
                        var classNamespaceAlias = GenerateNamespacePrefix(PreferredClassAlias);
                        writer.WriteNamespace(new NamespaceDeclaration(classNamespace, classNamespaceAlias));
                    }

                    // We may have consumed the first non-namespace node off the reader in order 
                    // to check it for being a NamespaceDeclaration. Make sure it still gets written.
                    if (!reader.IsEof)
                    {
                        writer.WriteNode(reader);
                    }
                }

                _rootLevelPrefixes = null; // not needed anymore
            }

            XamlServices.Transform(_pendingNodes.Reader, Writer._innerWriter, false);
            _pendingNodes = null;
        }

        private string GenerateNamespacePrefix(string desiredPrefix)
        {
            var aliasPostfix = string.Empty;
            // try postfixing 1-1000 first
            for (var i = 1; i <= 1000; i++)
            {
                var alias = desiredPrefix + aliasPostfix;
                if (!_rootLevelPrefixes.Contains(alias))
                {
                    return alias;
                }

                aliasPostfix = i.ToString(CultureInfo.InvariantCulture);
            }

            // fall back to GUID
            return desiredPrefix + Guid.NewGuid();
        }
    }

    // <ActivityBuilder>...</ActivityBuilder>
    private class BuilderClassNode : BuilderXamlNode
    {
        private List<KeyValuePair<string, XamlNodeQueue>> _defaultValueNodes;
        private XamlNodeQueue _otherNodes;
        private RootNode _rootNode;
        private XamlNodeQueue _xClassAttributeNodes;
        private string _xClassNamespace;
        private XamlNodeQueue _xClassNodes;
        private XamlType _xClassXamlType;
        private XamlNodeQueue _xPropertiesNodes;

        public BuilderClassNode(RootNode rootNode, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _rootNode = rootNode;

            // by default, if we're not in a special sub-tree, ferret the nodes away on the side
            _otherNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _otherNodes.Writer;
        }

        public void SetXClass(string builderName, XamlNodeQueue nameNodes)
        {
            _xClassNodes = new XamlNodeQueue(Writer.SchemaContext);
            _xClassNodes.Writer.WriteStartMember(XamlLanguage.Class);
            _xClassNamespace = null;
            var xClassName = builderName;
            if (string.IsNullOrEmpty(xClassName))
            {
                xClassName = string.Format(CultureInfo.CurrentCulture, "_{0}",
                    Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 4));
            }

            if (nameNodes != null)
            {
                XamlServices.Transform(nameNodes.Reader, _xClassNodes.Writer, false);
            }
            else
            {
                _xClassNodes.Writer.WriteValue(xClassName);
                _xClassNodes.Writer.WriteEndMember();
            }

            var nameStartIndex = xClassName.LastIndexOf('.');
            if (nameStartIndex > 0)
            {
                _xClassNamespace = builderName.Substring(0, nameStartIndex);
                xClassName = builderName.Substring(nameStartIndex + 1);
            }

            _xClassNamespace = string.Format(CultureInfo.CurrentUICulture, "clr-namespace:{0}",
                _xClassNamespace ?? string.Empty);
            _xClassXamlType = new XamlType(_xClassNamespace, xClassName, null, Writer.SchemaContext);
        }

        // Attributes [DependsOn("Name")]
        public void SetAttributes(XamlNodeQueue attributeNodes)
        {
            _xClassAttributeNodes = attributeNodes;
        }

        // Properties [DependsOn("Attributes")]
        public void SetProperties(XamlNodeQueue propertyNodes,
            List<KeyValuePair<string, XamlNodeQueue>> defaultValueNodes)
        {
            _xPropertiesNodes = propertyNodes;
            _defaultValueNodes = defaultValueNodes;

            // exiting the properties tag. So we've now accrued any instances of Name and Attributes
            // that could possibly be hit flush our preamble
            FlushPreamble();
        }

        private void FlushPreamble()
        {
            if (_otherNodes == null) // already flushed
            {
                return;
            }

            CurrentWriter = Writer._innerWriter;
            string classNamespace = null;
            // first, see if we need to emit a namespace corresponding to our class
            if (_defaultValueNodes != null)
            {
                classNamespace = _xClassNamespace;
            }

            _rootNode.FlushPendingNodes(classNamespace);
            _rootNode = null; // not needed anymore

            CurrentWriter.WriteStartObject(Writer._activityXamlType);

            // first dump x:Class
            if (_xClassNodes == null)
            {
                SetXClass(null, null); // this will setup a default
            }

            XamlServices.Transform(_xClassNodes.Reader, CurrentWriter, false);

            // String default values get written in attribute form immediately.
            // Other values get deferred until after x:Members, etc.
            XamlNodeQueue deferredPropertyNodes = null;
            if (_defaultValueNodes != null)
            {
                foreach (var defaultValueNode in _defaultValueNodes)
                {
                    var reader = defaultValueNode.Value.Reader;
                    if (reader.Read())
                    {
                        var isStringValue = false;
                        if (reader.NodeType == XamlNodeType.Value)
                        {
                            if (reader.Value is string stringValue)
                            {
                                isStringValue = true;
                            }
                        }

                        if (isStringValue)
                        {
                            CurrentWriter.WriteStartMember(new XamlMember(defaultValueNode.Key, _xClassXamlType, true));
                            CurrentWriter.WriteNode(reader);
                            XamlServices.Transform(defaultValueNode.Value.Reader, CurrentWriter, false);
                            // don't need an EndMember since it will be sitting in the node list (we only needed to strip the StartMember)                                
                        }
                        else
                        {
                            // Else: We'll write this out in a minute, after the x:ClassAttributes and x:Properties
                            if (deferredPropertyNodes == null)
                            {
                                deferredPropertyNodes = new XamlNodeQueue(Writer.SchemaContext);
                            }

                            deferredPropertyNodes.Writer.WriteStartMember(new XamlMember(defaultValueNode.Key,
                                _xClassXamlType, true));
                            deferredPropertyNodes.Writer.WriteNode(reader);
                            XamlServices.Transform(defaultValueNode.Value.Reader, deferredPropertyNodes.Writer, false);
                        }
                    }
                }
            }

            // then dump x:ClassAttributes if we have any
            if (_xClassAttributeNodes != null)
            {
                XamlServices.Transform(_xClassAttributeNodes.Reader, CurrentWriter, false);
            }

            // and x:Properties
            if (_xPropertiesNodes != null)
            {
                XamlServices.Transform(_xPropertiesNodes.Reader, CurrentWriter, false);
            }

            if (deferredPropertyNodes != null)
            {
                XamlServices.Transform(deferredPropertyNodes.Reader, CurrentWriter, false);
            }

            if (_otherNodes.Count > 0)
            {
                XamlServices.Transform(_otherNodes.Reader, CurrentWriter, false);
            }

            _otherNodes = null; // done with this
        }

        protected internal override void Complete()
        {
            if (_otherNodes != null)
                // need to flush
            {
                FlushPreamble();
            }
        }

        protected internal override void WriteStartMember(XamlMember xamlMember)
        {
            if (Writer._currentDepth == Depth + 1 && !xamlMember.IsAttachable)
            {
                if (xamlMember == Writer._activityBuilderName)
                {
                    // record that we're in ActivityBuilder.Name, since we'll need the class name for
                    // default value output
                    Writer.PushState(new BuilderNameNode(this, Writer));
                    return;
                }

                if (xamlMember == Writer._activityBuilderAttributes)
                {
                    // rewrite ActivityBuilder.Attributes to x:ClassAttributes
                    Writer.PushState(new AttributesNode(this, Writer));
                    return;
                }

                if (xamlMember == Writer._activityBuilderProperties)
                {
                    // rewrite ActivityBuilder.Properties to x:Members
                    Writer.PushState(new PropertiesNode(this, Writer));
                    return;
                }

                // any other member means we've passed properties due to [DependsOn] relationships
                FlushPreamble();
                if (xamlMember.DeclaringType == Writer._activityBuilderXamlType)
                {
                    // Rewrite "<ActivityBuilder.XXX>" to "<Activity.XXX>"
                    xamlMember = Writer._activityXamlType.GetMember(xamlMember.Name);
                    if (xamlMember == null)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(
                            SR.MemberNotSupportedByActivityXamlServices(xamlMember.Name)));
                    }

                    if (xamlMember.Name == "Implementation")
                    {
                        Writer.PushState(new ImplementationNode(Writer));
                    }
                }
            }

            base.WriteStartMember(xamlMember);
        }
    }

    // <ActivityBuilder.Name> node that we'll map to x:Class
    private class BuilderNameNode : BuilderXamlNode
    {
        private readonly BuilderClassNode _classNode;
        private readonly XamlNodeQueue _nameNodes;
        private string _builderName;

        public BuilderNameNode(BuilderClassNode classNode, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _classNode = classNode;
            _nameNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _nameNodes.Writer;
        }

        protected internal override void Complete()
        {
            _classNode.SetXClass(_builderName, _nameNodes);
        }

        protected internal override void WriteValue(object value)
        {
            if (Writer._currentDepth == Depth)
            {
                _builderName = (string) value;
            }

            base.WriteValue(value);
        }
    }

    // <ActivityBuilder.Attributes> node that we'll map to x:ClassAttributes
    private class AttributesNode : BuilderXamlNode
    {
        private readonly XamlNodeQueue _attributeNodes;
        private readonly BuilderClassNode _classNode;

        public AttributesNode(BuilderClassNode classNode, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _classNode = classNode;
            _attributeNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _attributeNodes.Writer;
            CurrentWriter.WriteStartMember(XamlLanguage.ClassAttributes);
        }

        protected internal override void Complete()
        {
            _classNode.SetAttributes(_attributeNodes);
        }
    }

    // <ActivityBuilder.Properties> node that we'll map to x:Members
    // since x:Members doesn't have GetObject/StartMember wrappers around the value, we need to eat those
    private class PropertiesNode : BuilderXamlNode
    {
        private readonly BuilderClassNode _classNode;
        private readonly XamlNodeQueue _propertiesNodes;
        private List<KeyValuePair<string, XamlNodeQueue>> _defaultValueNodes;
        private bool _skipGetObject;

        public PropertiesNode(BuilderClassNode classNode, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _classNode = classNode;
            _propertiesNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _propertiesNodes.Writer;
            CurrentWriter.WriteStartMember(XamlLanguage.Members);
        }

        protected internal override void WriteStartObject(XamlType xamlType)
        {
            if (xamlType == Writer._activityPropertyXamlType && Writer._currentDepth == Depth + 3)
            {
                xamlType = XamlLanguage.Property;
                Writer.PushState(new PropertyNode(this, Writer));
            }

            base.WriteStartObject(xamlType);
        }

        protected internal override void WriteGetObject()
        {
            if (Writer._currentDepth == Depth + 1)
            {
                _skipGetObject = true;
            }
            else
            {
                base.WriteGetObject();
            }
        }

        protected internal override void WriteEndObject()
        {
            if (_skipGetObject && Writer._currentDepth == Depth + 1)
            {
                _skipGetObject = false;
            }
            else
            {
                base.WriteEndObject();
            }
        }

        protected internal override void WriteStartMember(XamlMember xamlMember)
        {
            if (_skipGetObject && Writer._currentDepth == Depth + 2)
            {
                return;
            }

            base.WriteStartMember(xamlMember);
        }

        protected internal override void WriteEndMember()
        {
            if (_skipGetObject && Writer._currentDepth == Depth + 2)
            {
                return;
            }

            base.WriteEndMember();
        }

        protected internal override void Complete()
        {
            _classNode.SetProperties(_propertiesNodes, _defaultValueNodes);
        }

        public void AddDefaultValue(string propertyName, XamlNodeQueue value)
        {
            if (_defaultValueNodes == null)
            {
                _defaultValueNodes = new List<KeyValuePair<string, XamlNodeQueue>>();
            }

            if (string.IsNullOrEmpty(propertyName))
                // default a name if one doesn't exist
            {
                propertyName = string.Format(CultureInfo.CurrentCulture, "_{0}",
                    Guid.NewGuid().ToString().Replace("-", string.Empty));
            }

            _defaultValueNodes.Add(new KeyValuePair<string, XamlNodeQueue>(propertyName, value));
        }
    }

    // <DynamicActivityProperty>...</DynamicActivityProperty>
    private class PropertyNode : BuilderXamlNode
    {
        private readonly PropertiesNode _properties;
        private XamlNodeQueue _defaultValue;
        private string _propertyName;
        private XamlType _propertyType;

        public PropertyNode(PropertiesNode properties, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _properties = properties;
            CurrentWriter = properties.CurrentWriter;
        }

        public void SetName(string name)
        {
            _propertyName = name;
        }

        public void SetType(XamlType type)
        {
            _propertyType = type;
        }

        public void SetDefaultValue(XamlNodeQueue defaultValue)
        {
            _defaultValue = defaultValue;
        }

        protected internal override void WriteStartMember(XamlMember xamlMember)
        {
            if (xamlMember.DeclaringType == Writer._activityPropertyXamlType && Writer._currentDepth == Depth + 1)
            {
                if (xamlMember == Writer._activityPropertyName)
                {
                    // record that we're in a property name, since we'll need this for default value output
                    Writer.PushState(new PropertyNameNode(this, Writer));
                    xamlMember = DynamicActivityXamlReader.XPropertyName;
                }
                else if (xamlMember == Writer._activityPropertyType)
                {
                    // record that we're in a property type, since we'll need this for default value output
                    Writer.PushState(new PropertyTypeNode(this, Writer));
                    xamlMember = DynamicActivityXamlReader.XPropertyType;
                }
                else if (xamlMember == Writer._activityPropertyValue)
                {
                    // record that we're in a property value, since we'll need this for default value output.
                    // don't write anything since we'll dump the default values after we exit ActivityBuilder.Properties
                    Writer.PushState(new PropertyValueNode(this, Writer));
                    xamlMember = null;
                }
            }

            if (xamlMember != null)
            {
                base.WriteStartMember(xamlMember);
            }
        }

        protected internal override void Complete()
        {
            if (_defaultValue != null)
            {
                if (string.IsNullOrEmpty(_propertyName))
                    // default a name if one doesn't exist
                {
                    _propertyName = string.Format(CultureInfo.CurrentCulture, "_{0}",
                        Guid.NewGuid().ToString().Replace("-", string.Empty));
                }

                if (_defaultValue != null && _propertyType != null)
                    // post-process the default value nodes to strip out 
                    // StartObject+StartMember _Initialization+EndMember+EndObject 
                    // wrapper nodes if the type of the object matches the 
                    // property Type (since we are moving from "object Value" to "T Value"
                {
                    _defaultValue = StripTypeWrapping(_defaultValue, _propertyType);
                }

                _properties.AddDefaultValue(_propertyName, _defaultValue);
            }
        }

        private static XamlNodeQueue StripTypeWrapping(XamlNodeQueue valueNodes, XamlType propertyType)
        {
            var targetNodes = new XamlNodeQueue(valueNodes.Reader.SchemaContext);
            var source = valueNodes.Reader;
            var target = targetNodes.Writer;
            var depth = 0;
            var consumeWrapperEndTags = false;
            var hasBufferedStartObject = false;

            while (source.Read())
            {
                switch (source.NodeType)
                {
                    case XamlNodeType.StartObject:
                        depth++;
                        // only strip the wrapping type nodes if we have exactly this sequence:
                        // StartObject StartMember(Intialization) Value EndMember EndObject.
                        if (targetNodes.Count == 0 && depth == 1 && source.Type == propertyType &&
                            valueNodes.Count == 5)
                        {
                            hasBufferedStartObject = true;
                            continue;
                        }

                        break;

                    case XamlNodeType.GetObject:
                        depth++;
                        break;

                    case XamlNodeType.StartMember:
                        depth++;
                        if (hasBufferedStartObject)
                        {
                            if (depth == 2 && source.Member == XamlLanguage.Initialization)
                            {
                                consumeWrapperEndTags = true;
                                continue;
                            }

                            hasBufferedStartObject = false;
                            targetNodes.Writer.WriteStartObject(propertyType);
                        }

                        break;

                    case XamlNodeType.EndMember:
                        depth--;
                        if (consumeWrapperEndTags && depth == 1)
                        {
                            continue;
                        }

                        break;

                    case XamlNodeType.EndObject:
                        depth--;
                        if (consumeWrapperEndTags && depth == 0)
                        {
                            consumeWrapperEndTags = false;
                            continue;
                        }

                        break;
                }

                target.WriteNode(source);
            }

            return targetNodes;
        }
    }

    // <DynamicActivityProperty.Name>...</DynamicActivityProperty.Name>
    private class PropertyNameNode : BuilderXamlNode
    {
        private readonly PropertyNode _property;

        public PropertyNameNode(PropertyNode property, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _property = property;
            CurrentWriter = property.CurrentWriter;
        }

        protected internal override void WriteValue(object value)
        {
            if (Writer._currentDepth == Depth)
            {
                _property.SetName((string) value);
            }

            base.WriteValue(value);
        }
    }

    // <DynamicActivityProperty.Type>...</DynamicActivityProperty.Type>
    private class PropertyTypeNode : BuilderXamlNode
    {
        private readonly PropertyNode _property;

        public PropertyTypeNode(PropertyNode property, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _property = property;
            CurrentWriter = property.CurrentWriter;
        }

        protected internal override void WriteValue(object value)
        {
            if (Writer._currentDepth == Depth)
            {
                // We only support property type as an attribute
                var xamlTypeName = XamlTypeName.Parse(value as string, Writer._namespaceTable);
                var xamlType = Writer.SchemaContext.GetXamlType(xamlTypeName);
                _property.SetType(xamlType); // supports null
            }

            base.WriteValue(value);
        }
    }

    // <DynamicActivityProperty.Value>...</DynamicActivityProperty.Value>
    private class PropertyValueNode : BuilderXamlNode
    {
        private readonly PropertyNode _property;
        private readonly XamlNodeQueue _valueNodes;

        public PropertyValueNode(PropertyNode property, ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _property = property;
            _valueNodes = new XamlNodeQueue(writer.SchemaContext);
            CurrentWriter = _valueNodes.Writer;
        }

        protected internal override void Complete()
        {
            _property.SetDefaultValue(_valueNodes);
            base.Complete();
        }
    }

    // <ActivityBuilder.Implementation>...</ActivityBuilder.Implementation>
    // We need to convert any <ActivityBuilder.PropertyReferences> inside here into <PropertyReferenceExtension>.       
    private class ImplementationNode : BuilderXamlNode
    {
        private readonly Stack<ObjectFrame> _objectStack;

        public ImplementationNode(ActivityBuilderXamlWriter writer)
            : base(writer)
        {
            _objectStack = new Stack<ObjectFrame>();
        }

        internal void AddPropertyReference(ActivityPropertyReference propertyReference)
        {
            var currentFrame = _objectStack.Peek();
            Fx.Assert(currentFrame.Type != null, "Should only create PropertyReferencesNode inside a StartObject");
            if (currentFrame.PropertyReferences == null)
            {
                currentFrame.PropertyReferences = new List<ActivityPropertyReference>();
            }

            currentFrame.PropertyReferences.Add(propertyReference);
        }

        internal void SetUntransformedPropertyReferences(XamlMember propertyReferencesMember,
            XamlNodeQueue untransformedNodes)
        {
            var currentFrame = _objectStack.Peek();
            Fx.Assert(currentFrame.Type != null, "Should only create PropertyReferencesNode inside a StartObject");
            currentFrame.AddMember(propertyReferencesMember, untransformedNodes);
        }

        protected internal override void WriteStartMember(XamlMember xamlMember)
        {
            var currentFrame = _objectStack.Peek();
            if (currentFrame.Type == null)
            {
                base.WriteStartMember(xamlMember);
            }
            else if (xamlMember == Writer._activityBuilderPropertyReference ||
                     xamlMember == Writer._activityBuilderPropertyReferences)
                // Parse out the contents of <ActivityBuilder.PropertyReferences> using a PropertyReferencesNode
            {
                Writer.PushState(new PropertyReferencesNode(Writer, xamlMember, this));
            }
            else
            {
                CurrentWriter = currentFrame.StartMember(xamlMember, CurrentWriter);
            }
        }

        protected internal override void WriteStartObject(XamlType xamlType)
        {
            _objectStack.Push(new ObjectFrame {Type = xamlType});
            base.WriteStartObject(xamlType);
        }

        protected internal override void WriteGetObject()
        {
            _objectStack.Push(new ObjectFrame());
            base.WriteGetObject();
        }

        protected internal override void WriteEndObject()
        {
            var frame = _objectStack.Pop();
            frame.FlushMembers(CurrentWriter);
            base.WriteEndObject();
        }

        protected internal override void WriteEndMember()
        {
            // Stack can be empty here if this is the EndMember that closes out the Node
            var currentFrame = _objectStack.Count > 0 ? _objectStack.Peek() : null;
            if (currentFrame == null || currentFrame.Type == null)
            {
                base.WriteEndMember();
            }
            else
            {
                CurrentWriter = currentFrame.EndMember();
            }
        }

        private class ObjectFrame
        {
            private XamlNodeQueue _currentMemberNodes;
            private XamlWriter _parentWriter;

            public XamlType Type { get; set; }
            public XamlMember CurrentMember { get; set; }
            public List<KeyValuePair<XamlMember, XamlNodeQueue>> Members { get; set; }
            public List<ActivityPropertyReference> PropertyReferences { get; set; }

            public XamlWriter StartMember(XamlMember member, XamlWriter parentWriter)
            {
                CurrentMember = member;
                _parentWriter = parentWriter;
                _currentMemberNodes = new XamlNodeQueue(parentWriter.SchemaContext);
                return _currentMemberNodes.Writer;
            }

            public XamlWriter EndMember()
            {
                AddMember(CurrentMember, _currentMemberNodes);
                CurrentMember = null;
                _currentMemberNodes = null;
                var parentWriter = _parentWriter;
                _parentWriter = null;
                return parentWriter;
            }

            public void AddMember(XamlMember member, XamlNodeQueue content)
            {
                if (Members == null)
                {
                    Members = new List<KeyValuePair<XamlMember, XamlNodeQueue>>();
                }

                Members.Add(new KeyValuePair<XamlMember, XamlNodeQueue>(member, content));
            }

            public void FlushMembers(XamlWriter parentWriter)
            {
                if (Type == null)
                {
                    Fx.Assert(Members == null, "We shouldn't buffer members on GetObject");
                    return;
                }

                if (Members != null)
                {
                    foreach (var member in Members)
                    {
                        parentWriter.WriteStartMember(member.Key);
                        XamlServices.Transform(member.Value.Reader, parentWriter, false);
                        parentWriter.WriteEndMember();
                    }
                }

                if (PropertyReferences != null)
                {
                    foreach (var propertyReference in PropertyReferences)
                    {
                        var targetProperty = Type.GetMember(propertyReference.TargetProperty) ??
                            new XamlMember(propertyReference.TargetProperty, Type, false);
                        parentWriter.WriteStartMember(targetProperty);
                        WritePropertyReference(parentWriter, targetProperty, propertyReference.SourceProperty);
                        parentWriter.WriteEndMember();
                    }
                }
            }

            private void WritePropertyReference(XamlWriter parentWriter, XamlMember targetProperty,
                string sourceProperty)
            {
                var propertyReferenceType =
                    typeof(PropertyReferenceExtension<>).MakeGenericType(targetProperty.Type.UnderlyingType ??
                        typeof(object));
                var propertyReferenceXamlType = parentWriter.SchemaContext.GetXamlType(propertyReferenceType);
                parentWriter.WriteStartObject(propertyReferenceXamlType);

                if (sourceProperty != null)
                {
                    parentWriter.WriteStartMember(propertyReferenceXamlType.GetMember("PropertyName"));
                    parentWriter.WriteValue(sourceProperty);
                    parentWriter.WriteEndMember();
                }

                parentWriter.WriteEndObject();
            }
        }
    }

    // <ActivityBuilder.PropertyReference(s)> is stripped out and the inner
    // <ActivityPropertyReference>s map to PropertyReferenceNodes
    private class PropertyReferencesNode : BuilderXamlNode
    {
        private readonly XamlMember _originalStartMember;

        private readonly XamlNodeQueue
            _untransformedNodes; // nodes that couldn't be transformed to PropertyReference form

        public PropertyReferencesNode(ActivityBuilderXamlWriter writer, XamlMember originalStartMember,
            ImplementationNode parent)
            : base(writer)
        {
            _untransformedNodes = new XamlNodeQueue(Writer.SchemaContext);
            _originalStartMember = originalStartMember;
            Parent = parent;
            CurrentWriter = _untransformedNodes.Writer;
        }

        public bool HasUntransformedChildren { get; set; }

        public ImplementationNode Parent { get; }

        public XamlWriter UntransformedNodesWriter => _untransformedNodes.Writer;

        protected internal override void WriteStartObject(XamlType xamlType)
        {
            if (xamlType == Writer._activityPropertyReferenceXamlType)
            {
                Writer.PushState(new PropertyReferenceNode(Writer, this));
                return;
            }

            base.WriteStartObject(xamlType);
        }

        protected internal override void WriteEndMember()
        {
            // We only want the untransformedNodes writer to contain our member contents, not the
            // Start/End members, so don't write our closing EM
            if (Writer._currentDepth != Depth)
            {
                base.WriteEndMember();
            }
        }

        protected internal override void Complete()
        {
            if (HasUntransformedChildren)
                // Some ActivityPropertyReferences couldn't be transformed to properties. Leave them unchanged.
            {
                Parent.SetUntransformedPropertyReferences(_originalStartMember, _untransformedNodes);
            }
        }
    }

    // <ActivityPropertyReference TargetProperty="Foo" SourceProperty="RootActivityProperty"> maps to
    // <SomeClass.Foo><PropertyReference x:TypeArguments='targetType' PropertyName='RootActivityProperty'/></SomeClass.Foo>
    private class PropertyReferenceNode : BuilderXamlNode
    {
        private readonly PropertyReferencesNode _parent;
        private readonly XamlNodeQueue _propertyReferenceNodes;
        private bool _inSourceProperty;
        private bool _inTargetProperty;
        private string _sourceProperty;
        private string _targetProperty;

        public PropertyReferenceNode(ActivityBuilderXamlWriter writer, PropertyReferencesNode parent)
            : base(writer)
        {
            _propertyReferenceNodes = new XamlNodeQueue(writer.SchemaContext);
            _parent = parent;

            // save the untransformed output in case we're not able to perform the transformation
            CurrentWriter = _propertyReferenceNodes.Writer;
        }

        protected internal override void WriteStartMember(XamlMember xamlMember)
        {
            if (Writer._currentDepth == Depth + 1 // SM
                && xamlMember.DeclaringType == Writer._activityPropertyReferenceXamlType)
            {
                if (xamlMember.Name == "SourceProperty")
                {
                    _inSourceProperty = true;
                }
                else if (xamlMember.Name == "TargetProperty")
                {
                    _inTargetProperty = true;
                }
            }

            base.WriteStartMember(xamlMember); // save output just in case
        }

        protected internal override void WriteValue(object value)
        {
            if (_inSourceProperty)
            {
                _sourceProperty = (string) value;
            }
            else if (_inTargetProperty)
            {
                _targetProperty = (string) value;
            }

            base.WriteValue(value); // save output just in case
        }

        protected internal override void WriteEndMember()
        {
            if (Writer._currentDepth == Depth + 1)
            {
                _inSourceProperty = false;
                _inTargetProperty = false;
            }

            base.WriteEndMember(); // save output just in case
        }

        protected internal override void Complete()
        {
            if (_targetProperty == null)
            {
                // can't transform to <Foo.></Foo.>, dump original nodes <ActivityBuilder.PropertyReference(s) .../>
                _parent.HasUntransformedChildren = true;
                _parent.UntransformedNodesWriter.WriteStartObject(Writer._activityPropertyReferenceXamlType);
                XamlServices.Transform(_propertyReferenceNodes.Reader, _parent.UntransformedNodesWriter, false);
            }
            else
            {
                var propertyReference = new ActivityPropertyReference
                {
                    SourceProperty = _sourceProperty,
                    TargetProperty = _targetProperty
                };
                _parent.Parent.AddPropertyReference(propertyReference);
            }
        }
    }
}
