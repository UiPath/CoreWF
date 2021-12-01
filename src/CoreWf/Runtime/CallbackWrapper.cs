// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

using System;
using System.Activities.Internals;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

[DataContract]
internal class CallbackWrapper
{
    private static readonly BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static;

#if NET45
    static PermissionSet ReflectionMemberAccessPermissionSet = null; 
#endif

    private string _callbackName;
    private string _declaringAssemblyName;
    private string _declaringTypeName;
    private Delegate _callback;
    private ActivityInstance _activityInstance;

    protected internal CallbackWrapper() { }

    public CallbackWrapper(Delegate callback, ActivityInstance owningInstance)
    {
        ActivityInstance = owningInstance;
        _callback = callback;
    }

    public ActivityInstance ActivityInstance
    {
        get => _activityInstance;
        private set => _activityInstance = value;
    }

    protected bool IsCallbackNull => _callback == null && _callbackName == null;

    protected Delegate Callback => _callback;

    [DataMember(Name = "callbackName")]
    internal string SerializedCallbackName
    {
        get => _callbackName;
        set => _callbackName = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "declaringAssemblyName")]
    internal string SerializedDeclaringAssemblyName
    {
        get => _declaringAssemblyName;
        set => _declaringAssemblyName = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "declaringTypeName")]
    internal string SerializedDeclaringTypeName
    {
        get => _declaringTypeName;
        set => _declaringTypeName = value;
    }

    [DataMember(Name = "ActivityInstance")]
    internal ActivityInstance SerializedActivityInstance
    {
        get => ActivityInstance;
        set => ActivityInstance = value;
    }

    public static bool IsValidCallback(Delegate callback, ActivityInstance owningInstance)
    {
        Fx.Assert(callback != null, "This should only be called with non-null callbacks");

        object target = callback.Target;

        // if the target is null, it is static 
        if (target == null)
        {
            Fx.Assert(callback.Method.IsStatic, "This method should be static when target is null");
            return true;
        }

        // its owner's activity
        return ReferenceEquals(target, owningInstance.Activity);
    }

    // Special note about establishing callbacks:
    //
    // When establising a callback, we need to Assert ReflectionPermission(MemberAccess) because the callback
    // method will typically be a private method within a class that derives from Activity. Activity authors need
    // to be aware that their callback may be invoked with different permissions than were present when the Activity
    // was originally executed and perform appropriate security checks.
    //
    // We ensure that the declaring type of the callback method derives from Activity. This check is made in RecreateCallback.
    //
    // The classes that derive from CallbackWrapper and call EnsureCallback do an explicit cast of the returned delegate
    // to the delegate type that they expect before calling thru to the delegate. This cast is done in SecuritySafeCritical code.
    //
    // These checks are both made in Security[Safe]Critical code.

    [Fx.Tag.SecurityNote(Critical = "Because we are calling GenerateCallback, which are SecurityCritical.")]
    [SecurityCritical]
    protected void EnsureCallback(Type delegateType, Type[] parameterTypes, Type genericParameter)
    {
        // We were unloaded and have some work to do to rebuild the callback
        if (_callback == null)
        {
            _callback = GenerateCallback(delegateType, parameterTypes, genericParameter);
            Fx.Assert(_callback != null, "GenerateCallback should have been able to produce a non-null callback.");
        }
    }

    [Fx.Tag.SecurityNote(Critical = "Because we are calling GenerateCallback, which are SecurityCritical.",
        Safe = "Because the delegate is not leaked out of this routine. It is only validated.")]
    [SecuritySafeCritical]
    protected void ValidateCallbackResolution(Type delegateType, Type[] parameterTypes, Type genericParameter)
    {
        Fx.Assert(_callback != null && _callbackName != null, "We must have a callback and a callback name");

        if (!_callback.Equals(GenerateCallback(delegateType, parameterTypes, genericParameter)))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExecutionCallback(_callback.Method, null)));
        }
    }

    private MethodInfo FindMatchingGenericMethod(Type declaringType, Type[] parameterTypes, Type genericParameter)
    {
        MethodInfo[] potentialMatches = declaringType.GetMethods(bindingFlags);
        for (int i = 0; i < potentialMatches.Length; i++)
        {
            MethodInfo potentialMatch = potentialMatches[i];

            if (!potentialMatch.IsGenericMethod || potentialMatch.Name != _callbackName)
            {
                continue;
            }

            Fx.Assert(potentialMatch.IsGenericMethodDefinition, "We should be getting the generic method definition here.");

            if (potentialMatch.GetGenericArguments().Length != 1)
            {
                continue;
            }

            potentialMatch = potentialMatch.MakeGenericMethod(genericParameter);

            ParameterInfo[] parameters = potentialMatch.GetParameters();

            bool match = true;
            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                ParameterInfo parameter = parameters[parameterIndex];

                if (parameter.IsOut || parameter.IsOptional || parameter.ParameterType != parameterTypes[parameterIndex])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return potentialMatch;
            }
        }
        return null;
    }

    [Fx.Tag.SecurityNote(Critical = "Because we are calling RecreateCallback, which is SecurityCritical.")]
    [SecurityCritical]
    private Delegate GenerateCallback(Type delegateType, Type[] parameterTypes, Type genericParameter)
    {
        MethodInfo methodInfo = GetMatchingMethod(parameterTypes, out Type declaringType);

        if (methodInfo == null)
        {
            Fx.Assert(declaringType != null, "We must have found the declaring type.");
            methodInfo = FindMatchingGenericMethod(declaringType, parameterTypes, genericParameter);
        }

        if (methodInfo == null)
        {
            return null;
        }

        return RecreateCallback(delegateType, methodInfo);
    }

    [Fx.Tag.SecurityNote(Critical = "Because we are calling RecreateCallback, which is SecurityCritical.")]
    [SecurityCritical]
    protected void EnsureCallback(Type delegateType, Type[] parameters)
    {
        // We were unloaded and have some work to do to rebuild the callback
        if (_callback == null)
        {
            MethodInfo methodInfo = GetMatchingMethod(parameters, out _);

            Fx.Assert(methodInfo != null, "We must have a method info by now");

            _callback = RecreateCallback(delegateType, methodInfo);
        }
    }

    private MethodInfo GetMatchingMethod(Type[] parameters, out Type declaringType)
    {
        Fx.Assert(_callbackName != null, "This should only be called when there is actually a callback to run.");

        object targetInstance = ActivityInstance.Activity;

        if (_declaringTypeName == null)
        {
            declaringType = targetInstance.GetType();
        }
        else
        {
            // make a MethodInfo since it's not hanging directly off of our activity type
            Assembly callbackAssembly;
            if (_declaringAssemblyName != null)
            {
                callbackAssembly = Assembly.Load(_declaringAssemblyName);
            }
            else
            {
                callbackAssembly = targetInstance.GetType().Assembly;
            }

            declaringType = callbackAssembly.GetType(_declaringTypeName);
        }

        Fx.Assert(declaringType != null, "declaring type should be re-constructable from our serialized components");

        return declaringType.GetMethod(_callbackName, bindingFlags, null, parameters, null);
    }

    // The MethodInfo passed to this method must be derived
    [Fx.Tag.SecurityNote(Critical = "Because we are Asserting ReflectionPermission(MemberAccess) in order to get at private callback methods.")]
    [SecurityCritical]
    private Delegate RecreateCallback(Type delegateType, MethodInfo callbackMethod)
    {
        object targetInstance = null;

        // If the declaring type does not derive from Activity, somebody has manipulated the callback in the persistece store.
        if (!typeof(Activity).IsAssignableFrom(callbackMethod.DeclaringType))
        {
            return null;
        }

        if (!callbackMethod.IsStatic)
        {
            targetInstance = ActivityInstance.Activity;
        }

#if NET45
        // Asserting ReflectionPermission.MemberAccess because the callback method is most likely internal or private
        if (ReflectionMemberAccessPermissionSet == null)
        {
            PermissionSet myPermissionSet = new PermissionSet(PermissionState.None);
            myPermissionSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
            Interlocked.CompareExchange(ref ReflectionMemberAccessPermissionSet, myPermissionSet, null);
        }
        ReflectionMemberAccessPermissionSet.Assert(); 
#endif
        try
        {
            return Delegate.CreateDelegate(delegateType, targetInstance, callbackMethod);
        }
        finally
        {
#if NET45
            CodeAccessPermission.RevertAssert(); 
#endif
        }
    }

    [OnSerializing]
    //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.ReviewUnusedParameters)]
    //[SuppressMessage(FxCop.Category.Usage, "CA2238:ImplementSerializationMethodsCorrectly",
    //    Justification = "Needs to be internal for serialization in partial trust. We have set InternalsVisibleTo(System.Runtime.Serialization) to allow this.")]
    internal void OnSerializing(StreamingContext context)
    {
        if (_callbackName == null && !IsCallbackNull)
        {
            MethodInfo method = _callback.Method;
            _callbackName = method.Name;
            Type declaringType = method.DeclaringType;
            Type activityType = ActivityInstance.Activity.GetType();

            if (declaringType != activityType)
            {
                // If we're not directly off of the Activity type being used,
                // then we need to store the declaringType's name.
                _declaringTypeName = declaringType.FullName;

                if (declaringType.Assembly != activityType.Assembly)
                {
                    _declaringAssemblyName = declaringType.Assembly.FullName;
                }
            }

            if (method.IsGenericMethod)
            {
                OnSerializingGenericCallback();
            }
        }
    }

    protected virtual void OnSerializingGenericCallback()
    {
        // Generics are invalid by default
        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExecutionCallback(_callback.Method, null)));
    }
}
