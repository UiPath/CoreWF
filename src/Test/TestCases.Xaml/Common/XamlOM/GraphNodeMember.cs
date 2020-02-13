using System;
using System.Xml.Linq;
using TestObjects.Xaml.GraphCore;

namespace TestCases.Xaml.Common.XamlOM
{
    public class GraphNodeMember : GraphNodeXaml
    {

        public GraphNodeMember()
        {
            Properties.Add(GraphNodeXaml.BeforeWriteTraceProp,
                new Func<string>(() => GetBeforeTrace()));

            Properties.Add(GraphNodeXaml.AfterWriteTraceProp,
                new Func<string>(() => GetAfterTrace()));

        }

        private string GetBeforeTrace()
        {
            if (GetDoNotExpectProp(this))
            {
                return null;
            }
            else
            {
                return "Member:" + GetActualTypeName() + ":" + GetActualMemberName();
            }
        }

        private string GetAfterTrace()
        {
            if (GetDoNotExpectProp(this))
            {
                return null;
            }
            else
            {
                return "EndMember:" + GetActualTypeName() + ":" + GetActualMemberName();
            }
        }


        private string GetActualMemberName()
        {
            string alternateName = AlternateMemberName(this);
            if (!String.IsNullOrEmpty(alternateName))
            {
                return alternateName;
            }
            else
            {
                return MemberName;
            }
        }

        private XName GetActualTypeName()
        {
            XName alternateName = AlternateTypeName(this);
            if (alternateName != null)
            {
                return alternateName;
            }
            else
            {
                return this.TypeName;
            }
        }

        public string MemberName { get; set; }

        bool typeNameSpecified = false;
        XName typeName = null;
        public XName TypeName
        {
            get
            {
                return typeName;
            }

            set
            {
                if (value != null) typeNameSpecified = true;
                typeName = value;
            }
        }

        public MemberType MemberType { get; set; }

        public override void WriteBegin(IXamlWriter writer)
        {
            if (TypeName == null)
            {
                typeName = writer.GetCurrentType();
                writer.WriteStartMember(MemberName, this);
            }
            else
            {
                if (typeNameSpecified) writer.WriteStartMember(MemberName, TypeName, this);
                else writer.WriteStartMember(MemberName, this);
            }
        }

        public override void WriteEnd(IXamlWriter writer)
        {
            writer.WriteEndMember(this);
        }

        public const string AlternateMemberNameProp = "AlternateMemberName";
        public const string AlternateTypeNameProp = "AlternateTypeName";

        public static string AlternateMemberName(ITestDependencyObject props)
        {
            return (string)props.GetValue(AlternateMemberNameProp);
        }
        public static XName AlternateTypeName(ITestDependencyObject props)
        {
            return (XName)props.GetValue(AlternateTypeNameProp);
        }

        public const string DoNotExpectProp = "DoNotExpect";

        public static bool GetDoNotExpectProp(ITestDependencyObject props)
        {
            object value = props.GetValue(DoNotExpectProp);
            if (value == null)
                return false;
            else
                return (bool)value;
        }
    }

    public enum MemberType
    {
        Normal,
        Directive,
        Implicit
    };
}
