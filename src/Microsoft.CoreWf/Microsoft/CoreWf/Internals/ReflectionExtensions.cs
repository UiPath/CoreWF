// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.CoreWf.Internals
{
    internal static class ReflectionExtensions
    {
        #region Type
        public static Assembly Assembly(this Type type)
        {
            return type.GetTypeInfo().Assembly;
        }
        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
        public static bool ContainsGenericParameters(this Type type)
        {
            return type.GetTypeInfo().ContainsGenericParameters;
        }
        public static ConstructorInfo GetConstructor(this Type type, Type[] types)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingAttr, object binder, Type[] types, object[] modifiers)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static PropertyInfo GetProperty(this Type type, string name, BindingFlags bindingAttr)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }

        public static MethodInfo GetMethod(this Type type, string methodName, BindingFlags bindingFlags, Type[] parameterTypes, Type[] genericTypeArguments = null)
        {
            MethodInfo match = null;
            MethodInfo methodToMatch = null;
            MethodInfo[] methods = type.GetMethods(bindingFlags);
            foreach (MethodInfo method in methods)
            {
                methodToMatch = method;
                if (!string.Equals(methodToMatch.Name, methodName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // If there are genericTypeArguments, see if the method is generic and use the parameterInfo[]
                // for the method returned by MakeGenericMethod.
                if ((genericTypeArguments != null) && (genericTypeArguments.Length > 0))
                {
                    if (!methodToMatch.ContainsGenericParameters || (methodToMatch.GetGenericArguments().Length != genericTypeArguments.Length))
                    {
                        // not a match
                        continue;
                    }
                    try
                    {
                        methodToMatch = methodToMatch.MakeGenericMethod(genericTypeArguments);
                        if (methodToMatch == null)
                        {
                            continue;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Constraint violations will throw this exception--don't add to candidates
                        continue;
                    }
                }

                ParameterInfo[] methodParameters = methodToMatch.GetParameters();
                if (ParametersMatch(methodParameters, parameterTypes))
                {
                    match = methodToMatch;
                    break;
                }
                else
                {
                    continue;
                }
            }

            return match;
        }
        public static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }
        //public static bool IsAssignableFrom(this Type type, Type otherType)
        //{
        //    return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        //}
        public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }
        public static bool IsDefined(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().IsDefined(attributeType, inherit);
        }
        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }
        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }
        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }
        public static bool IsInstanceOfType(this Type type, object o)
        {
            return o == null ? false : type.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo());
        }
        public static bool IsMarshalByRef(this Type type)
        {
            return type.GetTypeInfo().IsMarshalByRef;
        }
        public static bool IsNotPublic(this Type type)
        {
            return type.GetTypeInfo().IsNotPublic;
        }
        public static bool IsSealed(this Type type)
        {
            return type.GetTypeInfo().IsSealed;
        }
        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }
        public static InterfaceMapping GetInterfaceMap(this Type type, Type interfaceType)
        {
            return type.GetTypeInfo().GetRuntimeInterfaceMap(interfaceType);
        }
        public static MemberInfo[] GetMember(this Type type, string name, BindingFlags bindingAttr)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static MemberInfo[] GetMembers(this Type type, BindingFlags bindingAttr)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }

        // TypeCode does not exist in N, but it is used by ServiceModel.
        // This extension method was copied from System.Private.PortableThunks\Internal\PortableLibraryThunks\System\TypeThunks.cs
        public static TypeCode GetTypeCode(this Type type)
        {
            if (type == null)
                return TypeCode.Empty;

            if (type == typeof(Boolean))
                return TypeCode.Boolean;

            if (type == typeof(Char))
                return TypeCode.Char;

            if (type == typeof(SByte))
                return TypeCode.SByte;

            if (type == typeof(Byte))
                return TypeCode.Byte;

            if (type == typeof(Int16))
                return TypeCode.Int16;

            if (type == typeof(UInt16))
                return TypeCode.UInt16;

            if (type == typeof(Int32))
                return TypeCode.Int32;

            if (type == typeof(UInt32))
                return TypeCode.UInt32;

            if (type == typeof(Int64))
                return TypeCode.Int64;

            if (type == typeof(UInt64))
                return TypeCode.UInt64;

            if (type == typeof(Single))
                return TypeCode.Single;

            if (type == typeof(Double))
                return TypeCode.Double;

            if (type == typeof(Decimal))
                return TypeCode.Decimal;

            if (type == typeof(DateTime))
                return TypeCode.DateTime;

            if (type == typeof(String))
                return TypeCode.String;

            if (type.GetTypeInfo().IsEnum)
                return GetTypeCode(Enum.GetUnderlyingType(type));

            return TypeCode.Object;
        }
        #endregion Type

        #region ConstructorInfo
        public static bool IsPublic(this ConstructorInfo ci)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static object Invoke(this ConstructorInfo ci, BindingFlags invokeAttr, object binder, object[] parameters, CultureInfo culture)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        #endregion ConstructorInfo

        #region MethodInfo, MethodBase
        public static RuntimeMethodHandle MethodHandle(this MethodBase mb)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static RuntimeMethodHandle MethodHandle(this MethodInfo mi)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        public static Type ReflectedType(this MethodInfo mi)
        {
            throw ExceptionHelper.PlatformNotSupported();
        }
        #endregion MethodInfo, MethodBase

        #region HelperMethods
        // If the ParameterInfo represents a "params" array, return the Type of that params array.
        // Otherwise, return null.
        private static Type ParamArrayType(ParameterInfo parameterInfo)
        {
            foreach (CustomAttributeData customAttribute in parameterInfo.CustomAttributes)
            {
                if (customAttribute.AttributeType == typeof(ParamArrayAttribute))
                {
                    return (parameterInfo.ParameterType.GetElementType());
                }
            }
            return null;
        }

        // Returns true if the type of the ParameterInfo matches parameterType or
        // if parameterType is null and the ParameterInfo has a default value (optional parameter)
        private static bool ParameterTypeMatch(ParameterInfo parameterInfo, Type parameterType)
        {
            if (parameterInfo.ParameterType == parameterType)
            {
                return true;
            }

            if ((parameterType == null) && parameterInfo.HasDefaultValue)
            {
                return true;
            }

            return false;
        }

        // Returns true if the last formal parameter (parameterInfos) matches the last of the parameterTypes,
        // taking into account the possibility that the last formal parameter is a "params" array.
        private static bool LastParameterInfoMatchesRemainingParameters(ParameterInfo[] parameterInfos, Type[] parameterTypes)
        {
            // The last parameter might NOT be a "params" array.
            if (parameterInfos[parameterInfos.Length - 1].ParameterType == parameterTypes[parameterInfos.Length - 1])
            {
                return true;
            }

            Type paramArrayType = ParamArrayType(parameterInfos[parameterInfos.Length - 1]);
            if (null == paramArrayType)
            {
                return false;
            }

            for (int i = parameterInfos.Length - 1; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] != paramArrayType)
                {
                    return false;
                }
            }

            return true;
        }

        // Returns true if the specified ParameterInfo[] (formal parameters)
        // matches the specified Type[] (actual parameters).
        // Takes into account optional parameters (with default values) and the last
        // formal parameter being a "params" array.
        private static bool ParametersMatch(ParameterInfo[] parameterInfos, Type[] parameterTypes)
        {
            // Most common case first - matching number of parameters.
            if (parameterInfos.Length == parameterTypes.Length)
            {
                // Special case = no parameters
                if (parameterInfos.Length == 0)
                {
                    return true;
                }

                // Check all but the last parameter. We will check the last parameter next as
                // a special case to check for a "params" array.
                for (int i = 0; i < parameterInfos.Length - 1; i++)
                {
                    if (!ParameterTypeMatch(parameterInfos[i], parameterTypes[i]))
                    {
                        return false;
                    }
                }

                // Now we need to check the last parameter. It might be a ParamArray, so we need to deal with that.
                if (!LastParameterInfoMatchesRemainingParameters(parameterInfos, parameterTypes))
                {
                    return false;
                }

                // The last parameter matches the ParamArray type
                return true;
            }

            // If the number of parameterTypes is LESS than the number of parameterInfos, then all the
            // types must match and the missing parameterInfos must have default values.
            if (parameterTypes.Length < parameterInfos.Length)
            {
                int i;
                for (i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterInfos[i].ParameterType != parameterTypes[i])
                    {
                        return false;
                    }
                }
                for (int j = i; j < parameterInfos.Length; j++)
                {
                    if (parameterInfos[j].HasDefaultValue)
                    {
                        continue;
                    }

                    // Only the last parameter is allowed to be a ParamArray.
                    if ((j < parameterInfos.Length - 1) || (null == ParamArrayType(parameterInfos[j])))
                    {
                        return false;
                    }
                }
                // if we get here, not all parameters were specified, but the missing ones have
                // default values or the last one is a ParamArray, so we have a match.
                return true;
            }

            // If we get here, the number of actual parameters is GREATER than the number of formal parameters.

            // If there are no formal parameters, we have no match.
            if (parameterInfos.Length == 0)
            {
                return false;
            }

            // Check all but the last parameter. We will check the last parameter next.
            for (int i = 0; i < parameterInfos.Length - 1; i++)
            {
                if (!ParameterTypeMatch(parameterInfos[i], parameterTypes[i]))
                {
                    return false;
                }
            }

            // Now we need to check the last formal parameter against the remaining actual parameters. It might be a ParamArray, so we need to deal with that.
            if (!LastParameterInfoMatchesRemainingParameters(parameterInfos, parameterTypes))
            {
                return false;
            }

            // The remaining parameters match the ParamArray type
            return true;
        }
        #endregion
    }
}
