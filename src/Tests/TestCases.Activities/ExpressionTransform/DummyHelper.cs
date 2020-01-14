// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;

namespace TestCases.Activities.ExpressionTransform
{
    public class DummyHelper
    {
        public static int StaticIntField = 7;
        public static int StaticIntProperty
        {
            get;
            set;
        }

        public static string StaticStringField = "Static String Field";
        public static string StaticStringField1 = "Static String Field";
        public static string StaticStringField2 = "Static String Field2";
        public static string StaticStringField3 = "Static String Field3";

        public string StringProperty { get; set; }

        private static string s_staticStringProp = "Static String Property";
        public static string StaticStringProperty
        {
            get
            {
                return s_staticStringProp;
            }
            set
            {
                s_staticStringProp = value;
            }
        }

        public static DateTime StaticDate = DateTime.FromBinary(0);
        public static TimeSpan StaticTimeSpan = TimeSpan.FromDays(1);

        public static int? StaticNullableIntField = 123;

        public static bool StaticBooleanField = true;
        public static bool StaticBooleanProperty
        {
            get;
            set;
        }

        // The naming convention is: typeof(T).Name + "Var"
        // See VariableHelper.cs GetVariable<T>
        public Variable<int> Int32Var;

        public Variable<String> StringVar;

        public Variable<DummyHelper> DummyHelperVar;

        public Variable<Boolean> BooleanVar;

        public static Variable<int?> StaticNullableIntVar = new Variable<int?>()
        {
            Name = "StaticNullableIntVar"
        };

        public int IntProperty
        {
            get;
            set;
        }

        public int IntField = 11;
        public string StringField = "String Field";

        public ValueTypeDummyHelper ValueTypeDummyHelperField = new ValueTypeDummyHelper();

        public int[,] TwoDimIntField = new int[3, 3];

        private int[][] _twoDimIntJaggedArrayField;
        public int[][] TwoDimIntJaggedArrayProperty
        {
            get
            {
                if (_twoDimIntJaggedArrayField == null)
                {
                    _twoDimIntJaggedArrayField = new int[3][];
                    for (int i = 0; i < 3; i++)
                    {
                        _twoDimIntJaggedArrayField[i] = new int[3];
                    }
                }

                return _twoDimIntJaggedArrayField;
            }
        }

        private readonly string[,,] _threeDimStringArray = new string[3, 3, 3];
        public string[,,] ThreeDimStringArrayProperty { get { return _threeDimStringArray; } }

        public static Dictionary<int, string> StaticDictionary = new Dictionary<int, string>()
        {
            {1, "one"},
            {2, "two"},
            {3, "three"},
            {4, "four"}
        };

        public static DummyHelper operator !(DummyHelper d1)
        {
            return d1;
        }

        public static DummyHelper operator +(DummyHelper d1, int d2)
        {
            return new DummyHelper
            {
                IntField = d1.IntField + d2
            };
        }

        public static DummyHelper operator +(int d2, DummyHelper d1)
        {
            return new DummyHelper
            {
                IntField = d1.IntField + d2
            };
        }

        public static DummyHelper Instance = new DummyHelper();

        public static int MethodCallWithoutArgument()
        {
            Console.WriteLine("MethodCallWithoutArgument");

            return 19;
        }

        public static int MethodCallWithArgument(int argument)
        {
            Console.WriteLine("MethodCallWithArgument");
            Console.WriteLine("Parameter: {0}", argument);

            return 23;
        }

        public static int MethodCallWithRefArgument(ref int argument)
        {
            argument = 29;

            return argument;
        }

        public static int MethodCallWithVarArg(params int[] argument)
        {
            return argument.Length;
        }

        public int InstanceFuncReturnInt()
        {
            return IntField;
        }

        public Func<int> InstanceDelegate;

        public static Variable NoneGenericVariable = new Variable<int>()
        {
            Name = "NoneGenericVariable"
        };

        public static Func<int, int> StaticDelegate = DummyHelper.MethodCallWithArgument;

        public static T GenericMethod<T>(T typeArgument)
        {
            return typeArgument;
        }

        public DummyHelper()
        {
            IntProperty = 19;
            InstanceDelegate = new Func<int>(InstanceFuncReturnInt);
        }

        private int[] _indexerField;
        private int[] IndexerProperty
        {
            get
            {
                if (_indexerField == null)
                    _indexerField = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };

                return _indexerField;
            }
        }

        public int this[int index]
        {
            get
            {
                return IndexerProperty[index];
            }
            set
            {
                IndexerProperty[index] = value;
            }
        }

        private int[,][][] _multiDimIntArrayOfJaggedArray;
        public int[,][][] MultiDimIntArrayOfJaggedArrayProperty
        {
            get
            {
                if (_multiDimIntArrayOfJaggedArray == null)
                {
                    _multiDimIntArrayOfJaggedArray = new int[3, 3][][];
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            _multiDimIntArrayOfJaggedArray[i, j] = new int[3][];
                            for (int x = 0; x < 3; x++)
                            {
                                _multiDimIntArrayOfJaggedArray[i, j][x] = new int[3];
                            }
                        }
                    }
                }

                return _multiDimIntArrayOfJaggedArray;
            }
        }

        public int this[int index1, int index2]
        {
            get
            {
                return IndexerProperty[index1 + index2];
            }
            set
            {
                IndexerProperty[index1 + index2] = value;
            }
        }

        public int this[int index1, float index2]
        {
            get
            {
                return IndexerProperty[index1 + (int)index2];
            }
            set
            {
                IndexerProperty[index1 + (int)index2] = value;
            }
        }

        public static int MethodCallWithVariousArgs(int intArg, string stringArg, ref int refArg, Variable<int?> genericArg, Func<int, int> delegateArg, DummyHelper instanceArg)
        {
            return 0;
        }
    }
}
