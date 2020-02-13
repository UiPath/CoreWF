using System.Xml.Linq;
using TestCases.Xaml.Common;
using TestCases.Xaml.Common.XamlOM;
using TestCases.Xaml.Driver.XamlReaderWriter;
using TestCases.Xaml.Types.Class;

namespace TestCases.Xaml.Types.XamlTemplate
{
    public static class XamlTemplateHostDocuments
    {
        public static XamlDocument GetHostDocument(string hostTypeName, string typeArguments, string templateMemberName, GraphNodeXaml template)
        {
            return new XamlDocument
            {
                Root = new GraphNodeRecord
                {
                    RecordName = XName.Get(hostTypeName, "clr-namespace:CDF.Test.TestCases.Xaml.Types.XamlTemplate;assembly=CDF.Test.TestCases.Xaml"),
                    Children =
                    {
                        new GraphNodeMember
                        {
                            MemberName = "TypeArguments",
                            //TypeName = XamlServices.DirectiveTypeName2006,
                            TypeName = Constants.Directive2006Type,
                            MemberType = MemberType.Directive,
                            Children =
                            {
                                new GraphNodeAtom
                                {
                                    Value = typeArguments
                                }
                            }
                        },
                        new GraphNodeMember
                        {
                            MemberName = "IntData",
                            Children =
                            {
                                new GraphNodeAtom
                                {
                                    Value = 42
                                }
                            }
                        },
                        GetClassData(),
                        new GraphNodeMember
                        {
                            MemberName = templateMemberName,
                            Children =
                            {
                                template
                            }
                        }
                    },
                    ExpectedNamespaces =
                    {
                        //XamlServices.NamespaceBuiltInTypes.NamespaceName,
                        Constants.Directive2006Type.NamespaceName,
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types;assembly=CDF.Test.TestCases.Xaml",
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types;assembly=CDF.Test.TestCases.Xaml, Version=4.0.0.0, Culture=neutral",
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types.IXmlSerializableTypes;assembly=CDF.Test.TestCases.Xaml",
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types.IXmlSerializableTypes;assembly=CDF.Test.TestCases.Xaml, Version=4.0.0.0, Culture=neutral",
                        "clr-namespace:System;assembly=mscorlib",
                        "clr-namespace:System.Collections.Generic;assembly=mscorlib",
                        "clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                        "clr-namespace:System.Collections;assembly=mscorlib",
                        "clr-namespace:System.Collections;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                    }
                }
            };
        }

        public static XamlDocument GetHostDocumentNoTypeArgs(string hostTypeName, string templateMemberName, GraphNodeXaml template)
        {
            return new XamlDocument
            {
                Root = new GraphNodeRecord
                {
                    RecordName = XName.Get(hostTypeName, "clr-namespace:CDF.Test.TestCases.Xaml.Types.XamlTemplate;assembly=CDF.Test.TestCases.Xaml"),
                    Children =
                    {
                        new GraphNodeMember
                        {
                            MemberName = "IntData",
                            Children =
                            {
                                new GraphNodeAtom
                                {
                                    Value = 42
                                }
                            }
                        },
                        GetClassData(),
                        new GraphNodeMember
                        {
                            MemberName = templateMemberName,
                            Children =
                            {
                                template
                            }
                        }
                    },
                    ExpectedNamespaces =
                    {
                        //XamlServices.NamespaceBuiltInTypes.NamespaceName,
                        Constants.Directive2006Type.NamespaceName,
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types;assembly=CDF.Test.TestCases.Xaml",
                        "clr-namespace:CDF.Test.TestCases.Xaml.Types;assembly=CDF.Test.TestCases.Xaml, Version=4.0.0.0, Culture=neutral",
                        "clr-namespace:System;assembly=mscorlib",
                        "clr-namespace:System.Collections.Generic;assembly=mscorlib",
                        "clr-namespace:System.Collections.Generic;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                        "clr-namespace:System.Collections;assembly=mscorlib",
                        "clr-namespace:System.Collections;assembly=mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                    }
                }
            };
        }



        static GraphNodeMember GetClassData()
        {
            return new GraphNodeMember
            {
                MemberName = "ClassData",
                Children =
                {
                    new GraphNodeRecord
                    {
                        //RecordName = XamlSchemaTypeResolver.Default.GetTypeReference(typeof(ClassType2)).Name,
                        RecordName = Constants.GetXNameFromType(typeof(ClassType2)),
                        Children =
                        {
                            new GraphNodeMember
                            {
                                MemberName = "Category",
                                Children =
                                {
                                    new GraphNodeRecord
                                    {
                                        //RecordName = XamlSchemaTypeResolver.Default.GetTypeReference(typeof(ClassType1)).Name,
                                        RecordName = Constants.GetXNameFromType(typeof(ClassType1)),
                                        Children =
                                        {
                                            new GraphNodeMember
                                            {
                                                MemberName = "Category",
                                                Children =
                                                {
                                                    new GraphNodeAtom
                                                    {
                                                        Value = "Some category"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }


    }
}
