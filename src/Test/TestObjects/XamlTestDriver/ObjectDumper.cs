// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace TestObjects.XamlTestDriver
{
    public class ObjectDumper
    {
        TextWriter writer = Console.Out;
        bool dumpFields = true;
        bool dumpProperties = true;

        string[] filteredMembers =
            {
                typeof(DataSet).FullName, "rowDiffId",
                typeof(DataSet).FullName, "RowDiffId",
            };
        string[] filteredTypes =
            {
                typeof(Pointer).FullName,
                typeof(CultureInfo).FullName,
                "System.Data.Index",
                "System.Data.DataColumnCollection",
            };
        int indent = 1;

        Hashtable objects;

        public ObjectDumper()
        {
            ReferenceComparer refComparer = new ReferenceComparer();
            this.objects = new Hashtable(refComparer);

        }

        public string DumpToString(string name, object o)
        {
            StringBuilder stringBuilder = new StringBuilder();
            this.writer = new StringWriter(stringBuilder);
            this.Dump(name, o);
            return stringBuilder.ToString();

        }

        public void Dump(string name, object o)
        {
            for (int i = 0; i < this.indent; i++)
            {
                this.writer.Write("--- ");
            }

            if (name == null)
            {
                name = string.Empty;
            }

            if (o == null)
            {
                this.writer.WriteLine(name + " = null");
                return;
            }

            Type type = o.GetType();

            this.writer.Write(type.Name + " " + name);

            if (this.objects[o] != null)
            {
                this.writer.WriteLine(" Existing " + type.Name + " object in graph");
                return;
            }
            if (((IList)this.filteredTypes).Contains(type.FullName))
            {
                this.writer.WriteLine(" <-- Type Filtered Out -->");
                return;
            }

            if (type != typeof(string))
            {
                this.objects.Add(o, o);
            }

            if (type.IsArray)
            {
                Array a = (Array)o;
                this.writer.WriteLine();
                this.indent++;
                for (int j = 0; j < a.Length; j++)
                {
                    Dump("[" + j + "]", a.GetValue(j));
                }
                this.indent--;
            }
            else if (o is Stream)
            {
                Stream stream = (Stream)o;
                int bytesRead = 0;
                byte[] buffer = new byte[256];
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        DumpBytes(buffer, 0, bytesRead);
                    }
                }
                while (bytesRead > 0);
            }
            else if (o is byte[])
            {
                byte[] bytes = (byte[])o;
                DumpBytes(bytes, 0, bytes.Length);
            }
            else if (o is XmlQualifiedName)
            {
                Dump("Name", ((XmlQualifiedName)o).Name);
                Dump("Namespace", ((XmlQualifiedName)o).Namespace);
            }
            else if (o is XmlNode)
            {
                string xml = ((XmlNode)o).OuterXml;
                xml = xml.Replace('\n', ' ');
                xml = xml.Replace('\r', ' ');
                this.writer.WriteLine(" = " + xml);
                return;
            }
            else if (type.IsEnum)
            {
                this.writer.WriteLine(" = " + ((Enum)o).ToString());
            }
            else if (type == (typeof(Decimal)))
            {
                this.writer.WriteLine(" = " + ((Decimal)o).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == (typeof(Single)))
            {
                this.writer.WriteLine(" = " + ((Single)o).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == (typeof(Double)))
            {
                this.writer.WriteLine(" = " + ((Double)o).ToString(CultureInfo.InvariantCulture));
            }
            else if (type.IsPrimitive)
            {
                this.writer.WriteLine(" = " + o.ToString());
            }
            else if (typeof(Exception).IsAssignableFrom(type))
            {
                this.writer.WriteLine(" = " + ((Exception)o).Message);
            }
            else if (o is string)
            {
                DumpString(o.ToString());
            }
            else if (o is Uri)
            {
                DumpString(o.ToString());
            }
            else if (o is DateTime)
            {
                DumpString(((DateTime)o).ToString(CultureInfo.InvariantCulture) + " Kind: " + ((DateTime)o).Kind.ToString());
            }
            else if (o is TimeSpan)
            {
                DumpString(o.ToString());
            }
            else if (o is Guid)
            {
                DumpString(o.ToString());
            }
            else if (o is Type)
            {
                DumpString(((Type)o).FullName);
            }
            else if (o is IEnumerable)
            {
                IEnumerator e = ((IEnumerable)o).GetEnumerator();
                if (e == null)
                {
                    this.writer.WriteLine(" GetEnumerator() == null");
                    return;
                }
                this.writer.WriteLine();
                int c = 0;
                this.indent++;
                while (e.MoveNext())
                {
                    Dump("[" + c + "]", e.Current);
                    c++;
                }
                this.indent--;
            }
            else
            {
                bool oldValue = this.dumpProperties;
                if (o is System.Runtime.Serialization.ExtensionDataObject)
                {
                    this.dumpProperties = false;
                }

                const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                this.writer.WriteLine();
                this.indent++;
                for (; type != null; type = type.BaseType)
                {
                    if (this.dumpFields)
                    {
                        FieldInfo[] fields = type.GetFields(bindingFlags);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            FieldInfo f = fields[i];
                            if (f.IsStatic)
                            {
                                continue;
                            }
                            if (IsMemberFiltered(type.FullName, f.Name))
                            {
                                continue;
                            }

                            Dump(f.Name, f.GetValue(o));
                        }
                    }
                    if (this.dumpProperties)
                    {
                        PropertyInfo[] props = type.GetProperties(bindingFlags);
                        for (int i = 0; i < props.Length; i++)
                        {
                            PropertyInfo p = props[i];
                            if (p.GetIndexParameters().Length != 0)
                            {
                                continue;
                            }
                            if (!p.CanRead)
                            {
                                continue;
                            }
                            if (!typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && !p.CanWrite)
                            {
                                continue;
                            }
                            if (p.PropertyType == type)
                            {
                                continue;
                            }
                            if (IsMemberFiltered(type.FullName, p.Name))
                            {
                                continue;
                            }

                            object v;
                            try
                            {
                                v = p.GetValue(o, null);
                            }
                            catch (Exception e) // jasonv - approved; could be any exception, dumps for later review
                            {
                                v = e;
                            }
                            Dump(p.Name, v);
                        }
                    }
                }
                this.indent--;

                if (o is System.Runtime.Serialization.ExtensionDataObject)
                {
                    this.dumpProperties = oldValue;
                }
            }
        }

        bool IsMemberFiltered(string typeName, string memberName)
        {
            for (int j = 0; j < this.filteredMembers.Length; j += 2)
            {
                if (this.filteredMembers[j] == typeName && this.filteredMembers[j + 1] == memberName)
                {
                    return true;
                }
            }
            return false;
        }

        void DumpString(string s)
        {
            if (s.Length > 40)
            {
                this.writer.WriteLine(" = ");
                this.writer.WriteLine("\"" + s + "\"");
            }
            else
            {
                this.writer.WriteLine(" = \"" + s + "\"");
            }
        }

        void DumpBytes(byte[] buf, int start, int len)
        {
            bool more = false;
            if (len > 256)
            {
                len = 256;
                more = true;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < (start + len); i++)
            {
                sb.Append(" ").Append(buf[i].ToString());
            }
            if (more)
            {
                sb.Append("...");
            }
            this.writer.WriteLine(sb.ToString());
        }

        public class ReferenceComparer : IEqualityComparer
        {
            public new bool Equals(Object x, Object y)
            {
                return x == y;
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
