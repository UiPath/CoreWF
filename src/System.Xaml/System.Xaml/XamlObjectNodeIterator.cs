//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xaml.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
using System.Xml;
using System.Reflection;
using System.Runtime.CompilerServices;
#if !NETSTANDARD1_0
using System.Xml.Serialization;
#endif

namespace System.Xaml
{
	internal class XamlObjectNodeIterator
	{
		static readonly XamlObject null_object = new XamlObject(XamlLanguage.Null, null);

		public XamlObjectNodeIterator(object root, XamlSchemaContext schemaContext, IValueSerializerContext vctx, XamlObjectReaderSettings settings)
		{
			ctx = schemaContext;
			this.root = root;
			value_serializer_ctx = vctx;
			this.settings = settings;
		}

		XamlObjectReaderSettings settings;
		XamlSchemaContext ctx;
		object root;
		IValueSerializerContext value_serializer_ctx;

		PrefixLookup PrefixLookup
		{
			get { return (PrefixLookup)value_serializer_ctx.GetService(typeof(INamespacePrefixLookup)); }
		}
		XamlNameResolver NameResolver
		{
			get { return (XamlNameResolver)value_serializer_ctx.GetService(typeof(IXamlNameResolver)); }
		}

		public XamlSchemaContext SchemaContext
		{
			get { return ctx; }
		}

		XamlType GetType(object obj)
		{
			return obj == null ? XamlLanguage.Null : ctx.GetXamlType(obj.GetType());
		}

		// returns StartObject, StartMember, Value, EndMember and EndObject. (NamespaceDeclaration is not included)
		public IEnumerable<XamlNodeInfo> GetNodes()
		{
			var xobj = new XamlObject(GetType(root), root);
			return GetNodes(null, xobj);
		}

		INamespacePrefixLookup prefixLookup;
		void LookupType(Type type)
		{
			var xamlType = SchemaContext.GetXamlType(type);
			prefixLookup = prefixLookup ?? value_serializer_ctx?.GetService(typeof(INamespacePrefixLookup)) as INamespacePrefixLookup;
			if (prefixLookup == null)
				return;
			prefixLookup.LookupPrefix(xamlType.PreferredXamlNamespace);
			if (xamlType.TypeArguments != null)
			{
				for (int i = 0; i < xamlType.TypeArguments.Count; i++)
				{
					prefixLookup.LookupPrefix(xamlType.TypeArguments[i].PreferredXamlNamespace);
				}
			}
		}

		IEnumerable<XamlNodeInfo> GetNodes(XamlMember xm, XamlObject xobj, XamlType overrideMemberType = null, bool partOfPositionalParameters = false, XamlNodeInfo node = null)
		{
			//If the item is invisible for the serialization then we must just skip it and return empty nodes info
			if (!xobj.Type.ShouldSerialize(xobj.Value)) yield break;

			object val;
			// Value - only for non-top-level node (thus xm != null)
			if (xm != null)
			{
				// collection items: each item is exposed as a standalone object that has StartObject, EndObject and contents.
				if (ReferenceEquals(xm, XamlLanguage.Items))
				{
					foreach (var xn in GetItemsNodes(xm, xobj))
						yield return xn;
					yield break;
				}

				// Arguments: each argument is written as a standalone object
				if (ReferenceEquals(xm, XamlLanguage.Arguments))
				{
					var xarg = new XamlObject();
					foreach (var argm in xobj.Type.GetSortedConstructorArguments())
					{
						var argv = argm.Invoker.GetValue(xobj.Value);
						xarg.Set(argm.Type, argv);
						foreach (var cn in GetNodes(null, xarg))
							yield return cn;
					}
					yield break;
				}

				// PositionalParameters: items are from constructor arguments, written as Value node sequentially. Note that not all of them are in simple string value. Also, null values are not written as NullExtension
				if (ReferenceEquals(xm, XamlLanguage.PositionalParameters))
				{
					var xarg = new XamlObject();
					foreach (var argm in xobj.Type.GetSortedConstructorArguments())
					{
						foreach (var cn in GetNodes(argm, xarg.Set(argm.Type, xobj.GetMemberValue(argm)), null, true))
							yield return cn;
					}
					yield break;
				}
				node = node ?? new XamlNodeInfo();

				if (ReferenceEquals(xm, XamlLanguage.Initialization))
				{
					yield return node.Set(TypeExtensionMethods.GetStringValue(xobj.Type, xm, xobj.Value, value_serializer_ctx));
					yield break;
				}

				val = xobj.Value;
				if (xm.DeferringLoader != null)
				{
					foreach (var xn in GetDeferredNodes(xm, val))
						yield return xn;
					yield break;
				}

				// don't serialize default values if one is explicitly specified using the DefaultValueAttribute
				if (!partOfPositionalParameters)
				{
					if (xm.Invoker.IsDefaultValue(val))
						yield break;
					if (settings.IgnoreDefaultValues && xm.DefaultValue == null)
					{
						if (xm.Type?.UnderlyingType?.GetTypeInfo().IsValueType == true)
						{
							if (Equals(val, Activator.CreateInstance(xm.Type.UnderlyingType)))
								yield break;
						}
						else if (ReferenceEquals(val, null))
							yield break;
					}
				}

				// overrideMemberType is (so far) used for XamlLanguage.Key.
				var xtt = overrideMemberType ?? xm.Type;
				if (!xtt.IsMarkupExtension && // this condition is to not serialize MarkupExtension whose type has TypeConverterAttribute (e.g. StaticExtension) as a string.
					(xtt.IsContentValue(value_serializer_ctx) || xm.IsContentValue(value_serializer_ctx)))
				{
					// though null value is special: it is written as a standalone object.

					if (val == null)
					{
						if (!partOfPositionalParameters)
							foreach (var xn in GetNodes(null, null_object, node: node))
								yield return xn;
						else
							yield return node.Set(String.Empty);
					}
					else if (!NameResolver.IsCollectingReferences) // for perf, getting string value can be expensive
						yield return node.Set(TypeExtensionMethods.GetStringValue(xtt, xm, val, value_serializer_ctx));
					else if (val is Type)
						LookupType((Type)val);
					//new XamlTypeName(xtt.SchemaContext.GetXamlType((Type)val)).ToString(value_serializer_ctx?.GetService(typeof(INamespacePrefixLookup)) as INamespacePrefixLookup);
					yield break;
				}

				// collection items: return GetObject and Items.
				if ((xm.Type.IsCollection || xm.Type.IsDictionary) && !xm.IsWritePublic)
				{
					yield return XamlNodeInfo.GetObject;
					// Write Items member only when there are items (i.e. do not write it if it is empty).
					var itemsValue = xobj.GetMemberObjectValue(XamlLanguage.Items);
					var en = GetItemsNodes(XamlLanguage.Items, itemsValue).GetEnumerator();
					if (en.MoveNext())
					{
						yield return node.Set(XamlNodeType.StartMember, XamlLanguage.Items);
						do
						{
							yield return en.Current;
						} while (en.MoveNext());
						yield return XamlNodeInfo.EndMember;
					}
					yield return XamlNodeInfo.EndObject;
					yield break;
				}
				if (xm.Type.IsXData)
				{
					var sw = new StringWriter();
					var xw = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Auto });
#if NETSTANDARD1_0
					if (!ReflectionHelpers.IXmlSerializableType?.GetTypeInfo().IsAssignableFrom(val?.GetType().GetTypeInfo()) ?? false)
						yield break; // do not output anything
					ReflectionHelpers.IXmlSerializableWriteXmlMethod?.Invoke(val, new object[] { xw });
#else
					var val3 = val as IXmlSerializable;
					if (val3 == null)
						yield break; // do not output anything
					val3.WriteXml(xw);
#endif
					xw.Dispose();
					var obj = new XData { Text = sw.ToString() };
					foreach (var xn in GetNodes(null, new XamlObject(XamlLanguage.XData, obj), node: node))
						yield return xn;
					yield break;
				}
			}
			else
			{
				node = node ?? new XamlNodeInfo();
				val = xobj.Value;
			}
			// Object - could become Reference
			if (val != null && !ReferenceEquals(xobj.Type, XamlLanguage.Reference))
			{
				if (xm != null && !xm.IsReadOnly && !IsPublicOrVisible(val.GetType()))
					throw new XamlObjectReaderException($"Cannot read from internal type {xobj.Type}");

				if (!xobj.Type.IsContentValue(value_serializer_ctx))
				{
					string refName = NameResolver.GetReferenceName(xobj, val);
					if (refName != null)
					{
						// The target object is already retrieved, so we don't return the same object again.
						NameResolver.SaveAsReferenced(val); // Record it as named object.
															// Then return Reference object instead.

						var xref = new XamlObject(XamlLanguage.Reference, new Reference(refName));
						yield return node.Set(XamlNodeType.StartObject, xref);
						yield return node.Set(XamlNodeType.StartMember, XamlLanguage.PositionalParameters);
						yield return node.Set(refName);
						yield return XamlNodeInfo.EndMember;
						yield return XamlNodeInfo.EndObject;
						yield break;
					}
					else
					{
						// The object appeared in the xaml tree for the first time. So we store the reference with a unique name so that it could be referenced later.
						NameResolver.SetNamedObject(val, true); // probably fullyInitialized is always true here.
					}
				}

				yield return node.Set(XamlNodeType.StartObject, xobj);

				// If this object is referenced and there is no [RuntimeNameProperty] member, then return Name property in addition.
				if (!NameResolver.IsCollectingReferences && xobj.Type.GetAliasedProperty(XamlLanguage.Name) == null)
				{
					string name = NameResolver.GetReferencedName(xobj, val);
					if (name != null)
					{
						yield return node.Set(XamlNodeType.StartMember, XamlLanguage.Name);
						yield return node.Set(name);
						yield return XamlNodeInfo.EndMember;
					}
				}
			}
			else
			{
				yield return node.Set(XamlNodeType.StartObject, xobj);
			}

			// get all object member nodes
			var xce = GetNodeMembers(xobj, value_serializer_ctx).GetEnumerator();
			var xobject = new XamlObject();
			var startNode = new XamlNodeInfo();
			while (xce.MoveNext())
			{
				var xnm = xce.Current;

				var en = GetNodes(xnm.Member, xnm.GetValue(xobject), node: node).GetEnumerator();
				if (en.MoveNext())
				{
					if (!xnm.Member.IsWritePublic && xnm.Member.Type != null && (xnm.Member.Type.IsCollection || xnm.Member.Type.IsDictionary))
					{
						// if we are a collection or dictionary without a setter, check to see if its empty first
						var node1 = en.Current.Copy(); // getObject
						if (!en.MoveNext())
							continue;
						var node2 = en.Current.Copy(); // possibly endObject
						if (!en.MoveNext()) // we have one more, so it's not empty!
							continue;

						// if we have three nodes, then it isn't empty

						yield return startNode.Set(XamlNodeType.StartMember, xnm.Member);
						yield return node1;
						yield return node2;
					}
					else
					{
						yield return startNode.Set(XamlNodeType.StartMember, xnm.Member);
					}

					do
					{
						yield return en.Current;
					} while (en.MoveNext());
					yield return XamlNodeInfo.EndMember;
				}
			}

			yield return XamlNodeInfo.EndObject;
		}

		bool IsPublicOrVisible(Type type)
		{
			if (type.GetTypeInfo().IsPublic)
				return true;
			if (settings.LocalAssembly != null)
			{
				var typeAssembly = type.GetTypeInfo().Assembly;
				if (settings.LocalAssembly == typeAssembly)
					return true;
				var typeName = typeAssembly.GetName();
				var internalsVisible = type.GetTypeInfo().Assembly.GetCustomAttributes<InternalsVisibleToAttribute>();
				foreach (var iv in internalsVisible)
				{
					var name = new AssemblyName(iv.AssemblyName);
					if (typeName.Matches(name))
						return true;
				}
			}
			return false;
		}

		IEnumerable<XamlNodeInfo> GetDeferredNodes (XamlMember xm, object val)
		{
			var node = new XamlNodeInfo();
			var reader = xm.DeferringLoader.ConverterInstance.Save(val, value_serializer_ctx);
			var xobj = new XamlObject();
			while (reader.Read())
			{
				var nodeType = reader.NodeType;
				switch (nodeType)
				{
					case XamlNodeType.StartObject:
					case XamlNodeType.GetObject:
						xobj.Set(reader.Type, reader.Value);
						yield return node.Set(nodeType, xobj);
						break;
					case XamlNodeType.EndObject:
						yield return XamlNodeInfo.EndObject;
						break;
					case XamlNodeType.StartMember:
						yield return node.Set(nodeType, reader.Member);
						break;
					case XamlNodeType.EndMember:
						yield return XamlNodeInfo.EndMember;
						break;
					case XamlNodeType.Value:
						yield return node.Set(reader.Value);
						break;
					case XamlNodeType.NamespaceDeclaration:
						yield return node.Set(reader.Namespace);
						break;
					default:
						break;
				}
			}
		}

		IEnumerable<XamlNodeMember> GetNodeMembers (XamlObject xobj, IValueSerializerContext vsctx)
		{
			var member = new XamlNodeMember();
			// XData.XmlReader is not returned.
			if (ReferenceEquals(xobj.Type, XamlLanguage.XData)) {
				yield return member.Set(xobj, XamlLanguage.XData.GetMember ("Text"));
				yield break;
			}

			// FIXME: find out why root Reference has PositionalParameters.
			if (xobj.Value != root && ReferenceEquals(xobj.Type, XamlLanguage.Reference))
				yield return member.Set(xobj, XamlLanguage.PositionalParameters);
			else {
				var inst = xobj.Value;
				var atts = new KeyValuePair<AttachableMemberIdentifier,object> [AttachablePropertyServices.GetAttachedPropertyCount (inst)];
				AttachablePropertyServices.CopyPropertiesTo (inst, atts, 0);
				XamlObject cobj = null;
				foreach (var p in atts) {
					var axt = ctx.GetXamlType (p.Key.DeclaringType);
					if (cobj == null)
						cobj = new XamlObject();
					yield return member.Set(cobj.Set(axt, p.Value), axt.GetAttachableMember (p.Key.MemberName));
				}

				var type = xobj.Type;
				if (type.HasPositionalParameters(vsctx))
				{
					yield return member.Set(xobj, XamlLanguage.PositionalParameters);
					yield break;
				}

				// Note that if the XamlType has the default constructor, we don't need "Arguments".
				IEnumerable<XamlMember> args = type.ConstructionRequiresArguments ? type.GetSortedConstructorArguments() : null;
				if (args != null && args.Any())
					yield return member.Set(xobj, XamlLanguage.Arguments);

				if (type.IsContentValue(vsctx))
				{
					yield return member.Set(xobj, XamlLanguage.Initialization);
					yield break;
				}

				if (type.IsDictionary)
				{
					yield return member.Set(xobj, XamlLanguage.Items);
					yield break;
				}

				var members = type.GetAllMembersAsList();
				for (int i = 0; i < members.Count; i++)
				{
					var m = members[i];
					// do not read constructor arguments twice (they are written inside Arguments).
					if (args != null && args.Contains(m))
						continue;
					// do not return non-public members (of non-collection/xdata). Not sure why .NET filters out them though.
					if (!m.IsReadPublic
					    || !m.ShouldSerialize(xobj.Value))
						continue;

					if (!m.IsWritePublic &&
						!m.Type.IsXData &&
						!m.Type.IsArray &&
						!m.Type.IsCollection &&
						!m.Type.IsDictionary)
						continue;

					yield return member.Set(xobj, m);
				}

				if (type.IsCollection)
					yield return member.Set(xobj, XamlLanguage.Items);
			}
		}

		IEnumerable<XamlNodeInfo> GetItemsNodes (XamlMember xm, XamlObject xobj)
		{
			var obj = xobj.Value;
			if (obj == null)
				yield break;
			var ie = xobj.Type.Invoker.GetItems (obj);
			var node = new XamlNodeInfo();
			var xiobj = new XamlObject();
			while (ie.MoveNext ()) {
				var iobj = ie.Current;
				// If it is dictionary, then retrieve the key, and rewrite the item as the Value part.
				object ikey = null;
				if (xobj.Type.IsDictionary) {
					Type kvpType = iobj.GetType ();
					bool isNonGeneric = kvpType == typeof (DictionaryEntry);
					var kp = isNonGeneric ? null : kvpType.GetRuntimeProperty ("Key");
					var vp = isNonGeneric ? null : kvpType.GetRuntimeProperty ("Value");
					ikey = isNonGeneric ? ((DictionaryEntry) iobj).Key : kp.GetValue (iobj, null);
					iobj = isNonGeneric ? ((DictionaryEntry) iobj).Value : vp.GetValue (iobj, null);
				}

				var wobj = TypeExtensionMethods.GetExtensionWrapped (iobj);
				xiobj.Set(GetType (wobj), wobj);
				if (ikey != null) {

					// TODO: do this without copying the XamlNodeInfo somehow?
					var en = GetNodes(null, xiobj).Select(c => c.Copy()).GetEnumerator();
					en.MoveNext();
					yield return en.Current; // StartObject

					//var nodes1 = en.Skip (1).Take (en.Count - 2);
					var nodes1 = new List<XamlNodeInfo>();
					while (en.MoveNext())
					{
						nodes1.Add(en.Current);
					}

					var nodes2 = GetKeyNodes (ikey, xobj.Type.KeyType);

					// group the members then sort to put the key nodes in the correct order
					var grouped = GroupMemberNodes (nodes1.Take(nodes1.Count - 1).Concat (nodes2)).OrderBy (r => r.Item1, TypeExtensionMethods.MemberComparer);
					foreach (var item in grouped) {
						foreach (var n in item.Item2)
							yield return n;
					}

					yield return nodes1[nodes1.Count - 1]; // EndObject
				}
				else
					foreach (var xn in GetNodes (null, xiobj, node: node))
						yield return xn;
			}
		}

		IEnumerable<XamlNodeInfo> GetMemberNodes(IEnumerator<XamlNodeInfo> e)
		{
			int nest = 1;
			yield return e.Current;
			while (e.MoveNext ()) {
				if (e.Current.NodeType == XamlNodeType.StartMember) {
					nest++;
				} else if (e.Current.NodeType == XamlNodeType.EndMember) {
					nest--;
					if (nest == 0) {
						yield return e.Current;
						break;
					}
				}
				yield return e.Current;
			}
		}

		IEnumerable<Tuple<XamlMember, IEnumerable<XamlNodeInfo>>> GroupMemberNodes(IEnumerable<XamlNodeInfo> nodes)
		{
			var e1 = nodes.GetEnumerator();

			while (e1.MoveNext())
			{
				if (e1.Current.NodeType == XamlNodeType.StartMember)
				{
					// split into chunks by member
					// TODO: Omit copying the nodes somehow?
					var member = e1.Current.Member;
					var memberNodes = (IEnumerable<XamlNodeInfo>)GetMemberNodes(e1).Select(r => r.Copy()).ToList();
					yield return Tuple.Create(member, memberNodes);
				}
				else
					throw new InvalidOperationException("Unexpected node");
			}
		}
		
		IEnumerable<XamlNodeInfo> GetKeyNodes (object ikey, XamlType keyType)
		{
			var node = new XamlNodeInfo();
			var en = GetNodes(XamlLanguage.Key, new XamlObject(GetType(ikey), ikey), keyType, false, node: node).GetEnumerator();
			if (en.MoveNext())
			{
				yield return new XamlNodeInfo(XamlNodeType.StartMember, XamlLanguage.Key);
				do
				{
					yield return en.Current;
				} while (en.MoveNext());

				yield return XamlNodeInfo.EndMember;
			}
		}

		// Namespace and Reference retrieval.
		// It is iterated before iterating the actual object nodes,
		// and results are cached for use in XamlObjectReader.
		public void PrepareReading ()
		{
			PrefixLookup.IsCollectingNamespaces = true;
			NameResolver.IsCollectingReferences = true;
			foreach (var xn in GetNodes ()) {
				if (xn.NodeType == XamlNodeType.GetObject)
					continue; // it is out of consideration here.
				if (xn.NodeType == XamlNodeType.StartObject) {
					LookupNamespacesInType(xn.Object.Type);
				} else if (xn.NodeType == XamlNodeType.StartMember) {
					var xm = xn.Member;
					// This filtering is done as a black list so far. There does not seem to be any usable property on XamlDirective.
					if (ReferenceEquals(xm, XamlLanguage.Items) 
						|| ReferenceEquals(xm, XamlLanguage.PositionalParameters)
						|| ReferenceEquals(xm, XamlLanguage.Initialization))
						continue;
					PrefixLookup.LookupPrefix (xn.Member.PreferredXamlNamespace);
				} else {
					if (xn.NodeType == XamlNodeType.Value && xn.Value is Type)
						// this tries to lookup existing prefix, and if there isn't any, then adds a new declaration.
						LookupType((Type)xn.Value);
						//TypeExtensionMethods.GetStringValue (XamlLanguage.Type, xn.Member, xn.Value, value_serializer_ctx);
					continue;
				}
			}
			PrefixLookup.Namespaces.Sort ((nd1, nd2) => String.CompareOrdinal (nd1.Prefix, nd2.Prefix));
			PrefixLookup.IsCollectingNamespaces = false;
			NameResolver.IsCollectingReferences = false;
			NameResolver.NameScopeInitializationCompleted (this);
		}
		
		void LookupNamespacesInType (XamlType xt)
		{
			PrefixLookup.LookupPrefix(xt.PreferredXamlNamespace);
			if (xt.TypeArguments != null) {
				// It is for x:TypeArguments
				PrefixLookup.LookupPrefix(XamlLanguage.Xaml2006Namespace);
				foreach (var targ in xt.TypeArguments)
					LookupNamespacesInType(targ);
			}
		}
	}
}
