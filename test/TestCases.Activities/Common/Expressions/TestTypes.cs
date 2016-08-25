// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace TestCases.Activities.Common.Expressions
{
    public class Complex
    {
        private int _real;
        private int _imaginary;

        public Complex()
        {
        }

        public Complex(int i, int j)
        {
            _real = i;
            _imaginary = j;
        }

        public int Real
        {
            get { return _real; }
            set { _real = value; }
        }

        public int Imaginary
        {
            get { return _imaginary; }
            set { _imaginary = value; }
        }

        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(c1._real + c2._real, c1._imaginary + c2._imaginary);
        }

        public static Complex operator -(Complex c1, Complex c2)
        {
            return new Complex(c1._real - c2._real, c1._imaginary - c2._imaginary);
        }

        public static Complex operator /(Complex c1, Complex c2)
        {
            return new Complex(c1._real / c2._real, c1._imaginary / c2._imaginary);
        }

        public static Complex operator *(Complex c1, Complex c2)
        {
            return new Complex(c1._real * c2._real, c1._imaginary * c2._imaginary);
        }

        public static bool operator ==(Complex c1, Complex c2)
        {
            if (Object.ReferenceEquals(c1, null) && Object.ReferenceEquals(c2, null))
                return true;

            if (Object.ReferenceEquals(c1, null) || Object.ReferenceEquals(c2, null))
                return false;

            return c1._real == c2._real && c1._imaginary == c2._imaginary;
        }

        public static bool operator !=(Complex c1, Complex c2)
        {
            return !(c1 == c2);
        }

        public static bool operator >(Complex c1, Complex c2)
        {
            return c1._real > c2._real;
        }

        public static bool operator <(Complex c1, Complex c2)
        {
            return c1._real < c2._real;
        }

        public static bool operator >=(Complex c1, Complex c2)
        {
            return c1._real >= c2._real;
        }

        public static bool operator <=(Complex c1, Complex c2)
        {
            return c1._real <= c2._real;
        }

        public static Complex operator |(Complex c1, Complex c2)
        {
            return new Complex(c1._real | c2._real, c1._imaginary | c2._imaginary);
        }

        public static Complex operator &(Complex c1, Complex c2)
        {
            return new Complex(c1._real & c2._real, c1._imaginary & c2._imaginary);
        }

        public static Complex operator !(Complex c1)
        {
            return new Complex(c1._real <= 0 ? 1 : 0, c1._imaginary <= 0 ? 1 : 0);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", _real, _imaginary);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PublicType
    {
        private int _privateField;

        public string publicField;

        public static int staticField;

        public int PublicProperty
        {
            get { return _privateField; }
            set { _privateField = value; }
        }

        public int WriteOnlyProperty
        {
            set { _privateField = value; }
        }

        public static int StaticProperty
        {
            get
            {
                if (staticField == 0)
                {
                    return 10;
                }
                else
                {
                    return staticField;
                }
            }
            set { staticField = value; }
        }

        private int PrivateProperty { get; set; }
    }

    public class ExceptionThrowingSetterAndGetter //We need separate type since xamlRoundtrip has to be disabled wherever we use this type (setter throws exception)
    {
        public int ExceptionThrowingProperty
        {
            get { throw new IndexOutOfRangeException(); }
            set { throw new IndexOutOfRangeException(); }
        }
    }

    public class ParameteredConstructorType
    {
        private Complex _number;

        public ParameteredConstructorType(Complex c)
        {
            _number = c;
        }

        public override string ToString()
        {
            if (_number == null)
            {
                return "null";
            }
            return _number.ToString();
        }
    }

    public class TypeWithOutParameterInConstructor
    {
        public TypeWithOutParameterInConstructor() { } //For xaml roundtripping

        public TypeWithOutParameterInConstructor(out int i)
        {
            i = 10;
        }
    }

    public class TypeWithRefParameterInConstructor
    {
        public TypeWithRefParameterInConstructor() { } //For xaml roundtripping

        public TypeWithRefParameterInConstructor(ref int i)
        {
            i = 10;
        }
    }

    public class TypeWithRefAndOutParametersinConstructor
    {
        public TypeWithRefAndOutParametersinConstructor() { }

        public TypeWithRefAndOutParametersinConstructor(int i, out int j, int k, ref int i1, ref int i2, out int i3)
        {
            j = i1 = i2 = i3 = 13;
        }
    }

    public class ParameterLessConstructorType
    {
        public ParameterLessConstructorType() { }

        public override bool Equals(object obj)
        {
            ParameterLessConstructorType p = obj as ParameterLessConstructorType;

            if (obj == null)
            {
                return false;
            }
            return true;
        }
        public override string ToString()
        {
            return "This type has parameterless constructor";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class OverLoadOperatorThrowingType
    {
        private int _value;
        private bool _flag = false;

        public OverLoadOperatorThrowingType()
        { }

        public OverLoadOperatorThrowingType(int value)
        {
            _value = value;
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public static bool ThrowException
        {
            get;
            set;
        }

        public static OverLoadOperatorThrowingType operator &(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new DivideByZeroException();
            }
            return new OverLoadOperatorThrowingType(item1._value & item2._value);
        }

        public static bool operator >(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value > item2._value;
        }

        public static bool operator <(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value < item2._value;
        }

        public static bool operator >=(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value >= item2._value;
        }

        public static bool operator <=(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value <= item2._value;
        }

        public static int operator |(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value | item2._value;
        }

        public static bool operator !=(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value != item2._value;
        }

        public static bool operator ==(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return item1._value == item2._value;
        }


        public static bool operator !(OverLoadOperatorThrowingType item)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return !item._flag;
        }

        public static OverLoadOperatorThrowingType operator /(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return new OverLoadOperatorThrowingType(item1._value / item2._value);
        }

        public static OverLoadOperatorThrowingType operator -(OverLoadOperatorThrowingType item1, OverLoadOperatorThrowingType item2)
        {
            if (ThrowException)
            {
                throw new ArithmeticException();
            }
            return new OverLoadOperatorThrowingType(item1._value - item2._value);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Base
    { }

    public class Derived : Base
    {
        public string MethodInDerivedType()
        {
            return "Ola";
        }

        public override bool Equals(object obj)
        {
            Derived d = obj as Derived;

            if (d == null)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct TheStruct
    {
        private int _privateField;
        public int publicField;
        public int publicField1;
        public System.IO.FileAccess enumField;
        public readonly int readonlyField;

        public TheStruct(int i) : this()
        {
            _privateField = 0;
            publicField = i;
        }

        public static int staticField;

        public static int StaticProperty
        {
            get { return 10; }
            set { staticField = value; }
        }

        public int PublicProperty
        {
            get;
            set;
        }

        public int PublicProperty1
        {
            get;
            set;
        }

        public int PropertyWithoutSetter
        {
            get
            {
                return _privateField;
            }
        }

        public int ThrowInSetterProperty
        {
            set
            {
                throw new Exception("ThrowInSetterProperty");
            }
        }

        private int PrivateProperty
        {
            get;
            set;
        }

        public System.IO.FileAccess EnumProperty
        {
            get;
            set;
        }

        public static string StaticStringProperty
        {
            get;
            set;
        }

        public override string ToString()
        {
            return publicField.ToString();
        }


        private int[] _oneDimIndexField;
        public int this[int indice]
        {
            get
            {
                if (_oneDimIndexField == null)
                    _oneDimIndexField = new int[100];

                return _oneDimIndexField[indice];
            }
            set
            {
                if (_oneDimIndexField == null)
                {
                    _oneDimIndexField = new int[100];
                }

                _oneDimIndexField[indice] = value;
            }
        }

        private int[,,] _multiDimIndexField;
        public int this[int i, int j, int k]
        {
            get
            {
                if (_multiDimIndexField == null)
                    _multiDimIndexField = new int[10, 10, 10];

                return _multiDimIndexField[i, j, k];
            }
            set
            {
                if (_multiDimIndexField == null)
                    _multiDimIndexField = new int[10, 10, 10];

                _multiDimIndexField[i, j, k] = value;
            }
        }

        public int this[float i]
        {
            set
            {
                throw new Exception();
            }
        }
    }

    public class TheClass
    {
        public string stringField;

        private int[] _oneDimIndexField = new int[100];
        public int this[int indice]
        {
            get
            {
                return _oneDimIndexField[indice];
            }
            set
            {
                _oneDimIndexField[indice] = value;
            }
        }

        private int[,,] _multiDimIndexField = new int[10, 10, 10];
        public int this[int i, int j, int k]
        {
            get
            {
                return _multiDimIndexField[i, j, k];
            }
            set
            {
                _multiDimIndexField[i, j, k] = value;
            }
        }

        private string[,] _twoDimStringIndexField = new string[100, 100];
        public string this[int i, int j]
        {
            get
            {
                return _twoDimStringIndexField[i, j];
            }
            set
            {
                _twoDimStringIndexField[i, j] = value;
            }
        }

        public int this[float i]
        {
            set
            {
                throw new Exception();
            }
        }

        public virtual int this[long i]
        {
            get
            {
                throw new Exception("Should not call to the indexer getter");
            }
            set
            {
                throw new Exception("Should not call to the indexer getter");
            }
        }

        public string StringProperty { get; set; }
    }

    public class ChildOfTheClass : TheClass
    {
        private int[] _oneDimIndexField = new int[100];
        public override int this[long i]
        {
            get
            {
                return _oneDimIndexField[i];
            }
            set
            {
                _oneDimIndexField[i] = value;
            }
        }
    }

    public enum WeekDay
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday
    }
}
