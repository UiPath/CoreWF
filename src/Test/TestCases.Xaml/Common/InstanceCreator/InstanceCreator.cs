// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using ReflectionMagic;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace TestCases.Xaml.Common.InstanceCreator
{
    public class InstanceCreator
    {
        public static object CreateInstanceOfArray(Type arrayType, Random rndGen)
        {
            Type type = arrayType.GetElementType();
            double rndNumber = rndGen.NextDouble();
            if (rndNumber < 0.01) return null; // 1% chance of null value
            int size = (int)Math.Pow(CreatorSettings.MaxArrayLength, rndNumber); // this will create more small arrays than large ones
            size--;
            Array result = Array.CreateInstance(type, size);
            for (int i = 0; i < size; i++)
            {
                result.SetValue(CreateInstanceOf(type, rndGen), i);
            }
            return result;
        }
        public static object CreateInstanceOfListOfT(Type listType, Random rndGen)
        {
            Type type = listType.GetGenericArguments()[0];
            double rndNumber = rndGen.NextDouble();
            if (rndNumber < 0.01) return null; // 1% chance of null value
            int size = (int)Math.Pow(CreatorSettings.MaxListLength, rndNumber); // this will create more small lists than large ones
            size--;
            object result = Activator.CreateInstance(listType);
            MethodInfo addMethod = listType.GetMethod("Add");
            for (int i = 0; i < size; i++)
            {
                addMethod.Invoke(result, new object[] { CreateInstanceOf(type, rndGen) });
            }
            return result;
        }
        public static object CreateInstanceOfNullableOfT(Type nullableOfTType, Random rndGen)
        {
            if (rndGen.Next(5) == 0) return null;
            Type type = nullableOfTType.GetGenericArguments()[0];
            return CreateInstanceOf(type, rndGen);
        }
        public static object CreateInstanceOfEnum(Type enumType, Random rndGen)
        {
            bool hasFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0;
            Array possibleValues = Enum.GetValues(enumType);
            if (!hasFlags)
            {
                return possibleValues.GetValue(rndGen.Next(possibleValues.Length));
            }
            else
            {
                int result = 0;
                if (rndGen.Next(10) > 0) //10% chance of value zero
                {
                    foreach (object value in possibleValues)
                    {
                        if (rndGen.Next(2) == 0)
                        {
                            result |= ((IConvertible)value).ToInt32(null);
                        }
                    }
                }
                return result;
            }
        }
        public static Array CreateInstanceOfSystemArray(Random rndGen)
        {
            Type[] memberTypes = new Type[] {
                typeof(string),
                typeof(int),
                typeof(long),
                typeof(byte),
                typeof(short),
                typeof(double),
                typeof(decimal),
                typeof(float),
                typeof(object),
                typeof(DateTime),
                typeof(TimeSpan),
                typeof(Guid),
                typeof(Uri),
                typeof(XmlQualifiedName),
            };
            double rndNumber = rndGen.NextDouble();
            if (rndNumber < 0.01) return null; // 1% chance of null value
            int size = (int)Math.Pow(CreatorSettings.MaxArrayLength, rndNumber); // this will create more small arrays than large ones
            size--;
            Array result = new object[size];
            for (int i = 0; i < size; i++)
            {
                Type elementType = memberTypes[rndGen.Next(memberTypes.Length)];
                result.SetValue(CreateInstanceOf(elementType, rndGen), i);
            }
            return result;
        }
        public static object CreateInstanceOfDictionaryOfKAndV(Type dictionaryType, Random rndGen)
        {
            Type[] genericArgs = dictionaryType.GetGenericArguments();
            Type typeK = genericArgs[0];
            Type typeV = genericArgs[1];
            double rndNumber = rndGen.NextDouble();
            if (rndNumber < 0.01) return null; // 1% chance of null value
            int size = (int)Math.Pow(CreatorSettings.MaxListLength, rndNumber); // this will create more small dictionaries than large ones
            size--;
            object result = Activator.CreateInstance(dictionaryType);
            MethodInfo addMethod = dictionaryType.GetMethod("Add");
            MethodInfo containsKeyMethod = dictionaryType.GetMethod("ContainsKey");
            for (int i = 0; i < size; i++)
            {
                object newKey = CreateInstanceOf(typeK, rndGen);
                bool containsKey = (bool)containsKeyMethod.Invoke(result, new object[] { newKey });
                if (!containsKey)
                {
                    object newValue = CreateInstanceOf(typeV, rndGen);
                    addMethod.Invoke(result, new object[] { newKey, newValue });
                }
            }
            return result;
        }
        internal static bool ContainsAttribute(MemberInfo member, Type attributeType)
        {
            object[] attributes = member.GetCustomAttributes(attributeType, false);
            return (attributes != null && attributes.Length > 0);
        }
        public static object CreateInstanceOf(Type type, Random rndGen)
        {
            if (type == typeof(Type))
            {
                return typeof(int);
            }
            if (type == typeof(System.Activities.Activity<bool>) || type == typeof(Activity))
                return new System.Activities.Expressions.Literal<bool>(true);
            if (PrimitiveCreator.CanCreateInstanceOf(type))
                return PrimitiveCreator.CreatePrimitiveInstance(type, rndGen);
            if (type.IsArray)
                return CreateInstanceOfArray(type, rndGen);
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return CreateInstanceOfNullableOfT(type, rndGen);
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                    return CreateInstanceOfListOfT(type, rndGen);
                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return CreateInstanceOfDictionaryOfKAndV(type, rndGen);
            }
            if (type == typeof(System.Array))
                return CreateInstanceOfSystemArray(rndGen);
            if (type.IsEnum)
                return CreateInstanceOfEnum(type, rndGen);
            if (ContainsAttribute(type, typeof(DataContractAttribute)))
                return DataContractInstanceCreator.CreateInstanceOf(type, rndGen);
            if (type == typeof(FlowNode))
            {
                return new FlowStep();
            }
            if (type.IsAbstract || type.IsSubclassOf(typeof(Delegate)))
            {
                return null;
            }
            if (type.IsPublic)
            {
                var result = POCOInstanceCreator.CreateInstanceOf(type, rndGen);
                if (result is Activity activity)
                {
                    activity.AsDynamic().Implementation = null;//orelse for example
                }
                return result;
            }
            return Activator.CreateInstance(type);
        }
    }
}
