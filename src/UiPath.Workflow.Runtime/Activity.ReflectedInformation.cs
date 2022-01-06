// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.ObjectModel;

namespace System.Activities;
using Internals;

public abstract partial class Activity
{
    internal class ReflectedInformation
    {
        private readonly Collection<RuntimeArgument> _arguments;
        private readonly Collection<Variable> _variables;
        private readonly Collection<Activity> _children;
        private readonly Collection<ActivityDelegate> _delegates;
        private static readonly Type DictionaryArgumentHelperType = typeof(DictionaryArgumentHelper<>);
        private static readonly Type OverloadGroupAttributeType = typeof(OverloadGroupAttribute);

        public ReflectedInformation(Activity owner)
            : this(owner, ReflectedType.All) { }

        private ReflectedInformation(Activity activity, ReflectedType reflectType)
        {
            // reflect over our activity and gather relevant pieces of the system so that the developer
            // doesn't need to worry about "zipping up" his model to the constructs necessary for the
            // runtime to function correctly
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(activity))
            {
                if ((reflectType & ReflectedType.Argument) == ReflectedType.Argument &&
                    ActivityUtilities.TryGetArgumentDirectionAndType(propertyDescriptor.PropertyType, out ArgumentDirection direction, out Type argumentType))
                {
                    // We only do our magic for generic argument types.  If the property is a non-generic
                    // argument type then that means the type of the RuntimeArgument should be based on
                    // the type of the argument bound to it.  The activity author is responsible for dealing
                    // with these dynamic typing cases.
                    if (propertyDescriptor.PropertyType.IsGenericType)
                    {
                        bool isRequired = GetIsArgumentRequired(propertyDescriptor);
                        List<string> overloadGroupNames = GetOverloadGroupNames(propertyDescriptor);
                        RuntimeArgument argument = new(propertyDescriptor.Name, argumentType, direction, isRequired, overloadGroupNames, propertyDescriptor, activity);
                        Add(ref _arguments, argument);
                    }
                }
                else if ((reflectType & ReflectedType.Variable) == ReflectedType.Variable &&
                    ActivityUtilities.IsVariableType(propertyDescriptor.PropertyType))
                {
                    if (propertyDescriptor.GetValue(activity) is Variable variable)
                    {
                        Add(ref _variables, variable);
                    }
                }
                else if ((reflectType & ReflectedType.Child) == ReflectedType.Child &&
                    ActivityUtilities.IsActivityType(propertyDescriptor.PropertyType))
                {
                    Activity workflowElement = propertyDescriptor.GetValue(activity) as Activity;
                    Add(ref _children, workflowElement);
                }
                else if ((reflectType & ReflectedType.ActivityDelegate) == ReflectedType.ActivityDelegate &&
                    ActivityUtilities.IsActivityDelegateType(propertyDescriptor.PropertyType))
                {
                    ActivityDelegate activityDelegate = propertyDescriptor.GetValue(activity) as ActivityDelegate;
                    Add(ref _delegates, activityDelegate);
                }
                else
                {
                    Type innerType;
                    bool foundMatch = false;
                    if ((reflectType & ReflectedType.Argument) == ReflectedType.Argument)
                    {
                        object property = propertyDescriptor.GetValue(activity);
                        if (property != null)
                        {
                            IList<RuntimeArgument> runtimeArguments = DictionaryArgumentHelper.TryGetRuntimeArguments(property, propertyDescriptor.Name);
                            if (runtimeArguments != null)
                            {
                                AddCollection(ref _arguments, runtimeArguments);
                                foundMatch = true;
                            }
                            else if (ActivityUtilities.IsArgumentDictionaryType(propertyDescriptor.PropertyType, out innerType))
                            {
                                Type concreteHelperType = DictionaryArgumentHelperType.MakeGenericType(innerType);
                                DictionaryArgumentHelper helper = Activator.CreateInstance(concreteHelperType, new object[] { property, propertyDescriptor.Name }) as DictionaryArgumentHelper;
                                AddCollection(ref _arguments, helper.RuntimeArguments);
                                foundMatch = true;
                            }
                        }
                    }

                    if (!foundMatch && ActivityUtilities.IsKnownCollectionType(propertyDescriptor.PropertyType, out innerType))
                    {
                        if ((reflectType & ReflectedType.Variable) == ReflectedType.Variable &&
                            ActivityUtilities.IsVariableType(innerType))
                        {
                            IEnumerable enumerable = propertyDescriptor.GetValue(activity) as IEnumerable;

                            AddCollection(ref _variables, enumerable);
                        }
                        else if ((reflectType & ReflectedType.Child) == ReflectedType.Child &&
                            ActivityUtilities.IsActivityType(innerType, false))
                        {
                            IEnumerable enumerable = propertyDescriptor.GetValue(activity) as IEnumerable;

                            AddCollection(ref _children, enumerable);
                        }
                        else if ((reflectType & ReflectedType.ActivityDelegate) == ReflectedType.ActivityDelegate &&
                            ActivityUtilities.IsActivityDelegateType(innerType))
                        {
                            IEnumerable enumerable = propertyDescriptor.GetValue(activity) as IEnumerable;

                            AddCollection(ref _delegates, enumerable);
                        }
                    }
                }
            }
        }

        public static Collection<RuntimeArgument> GetArguments(Activity parent)
        {
            Collection<RuntimeArgument> arguments = null;

            if (parent != null)
            {
                arguments = new ReflectedInformation(parent, ReflectedType.Argument).GetArguments();
            }

            arguments ??= new Collection<RuntimeArgument>();
            return arguments;
        }

        public static Collection<Variable> GetVariables(Activity parent)
        {
            Collection<Variable> variables = null;

            if (parent != null)
            {
                variables = new ReflectedInformation(parent, ReflectedType.Variable).GetVariables();
            }

            variables ??= new Collection<Variable>();
            return variables;
        }

        public static Collection<Activity> GetChildren(Activity parent)
        {
            Collection<Activity> children = null;

            if (parent != null)
            {
                children = new ReflectedInformation(parent, ReflectedType.Child).GetChildren();
            }

            children ??= new Collection<Activity>();
            return children;
        }

        public static Collection<ActivityDelegate> GetDelegates(Activity parent)
        {
            Collection<ActivityDelegate> delegates = null;

            if (parent != null)
            {
                delegates = new ReflectedInformation(parent, ReflectedType.ActivityDelegate).GetDelegates();
            }

            delegates ??= new Collection<ActivityDelegate>();
            return delegates;
        }

        public Collection<RuntimeArgument> GetArguments() => _arguments;

        public Collection<Variable> GetVariables() => _variables;

        public Collection<Activity> GetChildren() => _children;

        public Collection<ActivityDelegate> GetDelegates() => _delegates;

        private static void AddCollection<T>(ref Collection<T> list, IEnumerable enumerable)
            where T : class
        {
            if (enumerable != null)
            {
                foreach (object obj in enumerable)
                {
                    if (obj != null && obj is T t)
                    {
                        Add(ref list, t);
                    }
                }
            }
        }

        private static void Add<T>(ref Collection<T> list, T data)
        {
            if (data != null)
            {
                if (list == null)
                {
                    list = new Collection<T>();
                }
                list.Add(data);
            }
        }

        private static bool GetIsArgumentRequired(PropertyDescriptor propertyDescriptor) => propertyDescriptor.Attributes[typeof(RequiredArgumentAttribute)] != null;

        private static List<string> GetOverloadGroupNames(PropertyDescriptor propertyDescriptor)
        {
            List<string> overloadGroupNames = new(0);
            AttributeCollection propertyAttributes = propertyDescriptor.Attributes;
            for (int i = 0; i < propertyAttributes.Count; i++)
            {
                Attribute attribute = propertyAttributes[i];
                if (OverloadGroupAttributeType.IsAssignableFrom(attribute.GetType()))
                {
                    overloadGroupNames.Add(((OverloadGroupAttribute)attribute).GroupName);
                }
            }
            return overloadGroupNames;
        }

        [Flags]
        private enum ReflectedType
        {
            Argument = 0X1,
            Variable = 0X2,
            Child = 0X4,
            ActivityDelegate = 0X8,
            All = 0XF
        }

        private class DictionaryArgumentHelper
        {
            protected DictionaryArgumentHelper() { }

            public IList<RuntimeArgument> RuntimeArguments { get; protected set; }

            public static IList<RuntimeArgument> TryGetRuntimeArguments(object propertyValue, string propertyName)
            {
                // special case each of the non-generic argument types to avoid reflection costs

                return propertyValue switch
                {
                    IEnumerable<KeyValuePair<string, Argument>> argumentEnumerable => GetRuntimeArguments(argumentEnumerable, propertyName),
                    IEnumerable<KeyValuePair<string, InArgument>> inArgumentEnumerable => GetRuntimeArguments(inArgumentEnumerable, propertyName),
                    IEnumerable<KeyValuePair<string, OutArgument>> outArgumentEnumerable => GetRuntimeArguments(outArgumentEnumerable, propertyName),
                    IEnumerable<KeyValuePair<string, InOutArgument>> inOutArgumentEnumerable => GetRuntimeArguments(inOutArgumentEnumerable, propertyName),
                    _ => null
                };
            }

            protected static IList<RuntimeArgument> GetRuntimeArguments<T>(IEnumerable<KeyValuePair<string, T>> argumentDictionary, string propertyName) where T : Argument
            {
                IList<RuntimeArgument> runtimeArguments = new List<RuntimeArgument>();

                foreach (KeyValuePair<string, T> pair in argumentDictionary)
                {
                    string key = pair.Key;
                    Argument value = pair.Value;

                    if (value == null)
                    {
                        string argName = key ?? "<null>";
                        throw FxTrace.Exception.AsError(new ValidationException(SR.MissingArgument(argName, propertyName)));
                    }
                    if (string.IsNullOrEmpty(key))
                    {
                        throw FxTrace.Exception.AsError(new ValidationException(SR.MissingNameProperty(value.ArgumentType)));
                    }

                    RuntimeArgument runtimeArgument = new(key, value.ArgumentType, value.Direction, false, null, value);
                    runtimeArguments.Add(runtimeArgument);
                }

                return runtimeArguments;
            }
        }

        private class DictionaryArgumentHelper<T> : DictionaryArgumentHelper where T : Argument
        {
            public DictionaryArgumentHelper(object propertyValue, string propertyName)
                : base()
            {
                IEnumerable<KeyValuePair<string, T>> argumentDictionary = propertyValue as IEnumerable<KeyValuePair<string, T>>;

                RuntimeArguments = GetRuntimeArguments(argumentDictionary, propertyName);
            }
        }
    }
}
