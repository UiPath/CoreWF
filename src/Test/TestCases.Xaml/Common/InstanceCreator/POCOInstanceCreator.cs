using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestCases.Xaml.Common.InstanceCreator
{
    public static class POCOInstanceCreator
    {
        private static int CompareMembers(MemberInfo member1, MemberInfo member2)
        {
            return member1.Name.CompareTo(member2.Name);
        }
        private static void FilterIgnoredDataMembers<T>(List<T> list) where T : MemberInfo
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                object[] customAttributes = ((MemberInfo)list[i]).GetCustomAttributes(false);
                foreach (object attribute in customAttributes)
                {
                    if (attribute != null && attribute.GetType().Name == "IgnoreDataMemberAttribute")
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        private static void SetPublicFields(Type dcType, object obj, Random rndGen)
        {
            List<FieldInfo> fields = new List<FieldInfo>(dcType.GetFields(BindingFlags.Public | BindingFlags.Instance));
            FilterIgnoredDataMembers<FieldInfo>(fields);
            fields.Sort(new Comparison<FieldInfo>(CompareMembers));
            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttributes(typeof(IgnoreMemberAttribute), false).Length == 0)
                {
                    //Set the new value only if the value was not set by default.
                    object fieldValue = InstanceCreator.CreateInstanceOf(field.FieldType, rndGen);
                    field.SetValue(obj, fieldValue);
                }

            }
        }
        private static void SetPublicProperties(Type dcType, object obj, Random rndGen)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>(dcType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            FilterIgnoredDataMembers<PropertyInfo>(properties);
            properties.Sort(new Comparison<PropertyInfo>(CompareMembers));
            foreach (PropertyInfo property in properties)
            {
                if (property.GetSetMethod() == null || property.Name == "EvaluationOrder" || property.PropertyType.Name.StartsWith("OutArgument"))
                    continue;
                object propertyValue = InstanceCreator.CreateInstanceOf(property.PropertyType, rndGen);
                property.SetValue(obj, propertyValue, null);
            }
        }
        public static object CreateInstanceOf(Type pocoType, Random rndGen)
        {
            object result = null;
            if (rndGen.NextDouble() < 0.01 && !pocoType.IsValueType)
            {
                // 1% chance of null object, if it is not a struct
                return null;
            }
            ConstructorInfo randomConstructor = pocoType.GetConstructor(new Type[] { typeof(Random) });
            if (randomConstructor != null)
            {
                result = randomConstructor.Invoke(new object[] { rndGen });
            }
            else
            {
                ConstructorInfo defaultConstructor = pocoType.GetConstructor(new Type[0]);
                if (defaultConstructor != null || pocoType.IsValueType)
                {
                    if (defaultConstructor != null)
                    {
                        result = defaultConstructor.Invoke(new object[0]);
                    }
                    else
                    {
                        result = Activator.CreateInstance(pocoType);
                    }
                    SetPublicFields(pocoType, result, rndGen);
                    SetPublicProperties(pocoType, result, rndGen);
                }
                else
                {

                    throw new ArgumentException("Don't know how to create an instance of " + pocoType.FullName);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Apply this attribute to skip a field while creating instance
    /// </summary>
    public class IgnoreMemberAttribute : Attribute
    {
    }
}
