using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace CoreWf
{
    internal class ReflectedInformation
    {
        Activity parent;

        Collection<RuntimeArgument> arguments;
        Collection<Variable> variables;
        Collection<Activity> children;
        Collection<ActivityDelegate> delegates;

        static Type DictionaryArgumentHelperType = typeof(DictionaryArgumentHelper<>);
        static Type OverloadGroupAttributeType = typeof(OverloadGroupAttribute);

        public ReflectedInformation(Activity owner)
            : this(owner, ReflectedType.All)
        {
        }

        ReflectedInformation(Activity activity, ReflectedType reflectType)
        {
            this.parent = activity;

            // reflect over our activity and gather relevant pieces of the system so that the developer
            // doesn't need to worry about "zipping up" his model to the constructs necessary for the
            // runtime to function correctly

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(activity))
            {
                ArgumentDirection direction;
                Type argumentType;
                if ((reflectType & ReflectedType.Argument) == ReflectedType.Argument &&
                    ActivityUtilities.TryGetArgumentDirectionAndType(propertyDescriptor.PropertyType, out direction, out argumentType))
                {
                    // We only do our magic for generic argument types.  If the property is a non-generic
                    // argument type then that means the type of the RuntimeArgument should be based on
                    // the type of the argument bound to it.  The activity author is responsible for dealing
                    // with these dynamic typing cases.
                    if (propertyDescriptor.PropertyType.GetTypeInfo().IsGenericType)
                    {
                        bool isRequired = GetIsArgumentRequired(propertyDescriptor);
                        List<string> overloadGroupNames = GetOverloadGroupNames(propertyDescriptor);
                        RuntimeArgument argument = new RuntimeArgument(propertyDescriptor.Name, argumentType, direction, isRequired, overloadGroupNames, propertyDescriptor, activity);
                        Add<RuntimeArgument>(ref this.arguments, argument);
                    }
                }
                else if ((reflectType & ReflectedType.Variable) == ReflectedType.Variable &&
                    ActivityUtilities.IsVariableType(propertyDescriptor.PropertyType))
                {
                    Variable variable = propertyDescriptor.GetValue(activity) as Variable;
                    if (variable != null)
                    {
                        Add<Variable>(ref this.variables, variable);
                    }
                }
                else if ((reflectType & ReflectedType.Child) == ReflectedType.Child &&
                    ActivityUtilities.IsActivityType(propertyDescriptor.PropertyType))
                {
                    Activity workflowElement = propertyDescriptor.GetValue(activity) as Activity;
                    Add<Activity>(ref this.children, workflowElement);
                }
                else if ((reflectType & ReflectedType.ActivityDelegate) == ReflectedType.ActivityDelegate &&
                    ActivityUtilities.IsActivityDelegateType(propertyDescriptor.PropertyType))
                {
                    ActivityDelegate activityDelegate = propertyDescriptor.GetValue(activity) as ActivityDelegate;
                    Add<ActivityDelegate>(ref this.delegates, activityDelegate);
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
                                this.AddCollection(ref this.arguments, runtimeArguments);
                                foundMatch = true;
                            }
                            else if (ActivityUtilities.IsArgumentDictionaryType(propertyDescriptor.PropertyType, out innerType))
                            {
                                Type concreteHelperType = DictionaryArgumentHelperType.MakeGenericType(innerType);
                                DictionaryArgumentHelper helper = Activator.CreateInstance(concreteHelperType, new object[] { property, propertyDescriptor.Name }) as DictionaryArgumentHelper;
                                this.AddCollection(ref this.arguments, helper.RuntimeArguments);
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

                            AddCollection(ref this.variables, enumerable);
                        }
                        else if ((reflectType & ReflectedType.Child) == ReflectedType.Child &&
                            ActivityUtilities.IsActivityType(innerType, false))
                        {
                            IEnumerable enumerable = propertyDescriptor.GetValue(activity) as IEnumerable;

                            AddCollection(ref this.children, enumerable);
                        }
                        else if ((reflectType & ReflectedType.ActivityDelegate) == ReflectedType.ActivityDelegate &&
                            ActivityUtilities.IsActivityDelegateType(innerType))
                        {
                            IEnumerable enumerable = propertyDescriptor.GetValue(activity) as IEnumerable;

                            AddCollection(ref this.delegates, enumerable);
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

            if (arguments == null)
            {
                arguments = new Collection<RuntimeArgument>();
            }

            return arguments;
        }

        public static Collection<Variable> GetVariables(Activity parent)
        {
            Collection<Variable> variables = null;

            if (parent != null)
            {
                variables = new ReflectedInformation(parent, ReflectedType.Variable).GetVariables();
            }

            if (variables == null)
            {
                variables = new Collection<Variable>();
            }

            return variables;
        }

        public static Collection<Activity> GetChildren(Activity parent)
        {
            Collection<Activity> children = null;

            if (parent != null)
            {
                children = new ReflectedInformation(parent, ReflectedType.Child).GetChildren();
            }

            if (children == null)
            {
                children = new Collection<Activity>();
            }

            return children;
        }

        public static Collection<ActivityDelegate> GetDelegates(Activity parent)
        {
            Collection<ActivityDelegate> delegates = null;

            if (parent != null)
            {
                delegates = new ReflectedInformation(parent, ReflectedType.ActivityDelegate).GetDelegates();
            }

            if (delegates == null)
            {
                delegates = new Collection<ActivityDelegate>();
            }

            return delegates;
        }

        public Collection<RuntimeArgument> GetArguments()
        {
            return this.arguments;
        }

        public Collection<Variable> GetVariables()
        {
            return this.variables;
        }

        public Collection<Activity> GetChildren()
        {
            return this.children;
        }

        public Collection<ActivityDelegate> GetDelegates()
        {
            return this.delegates;
        }

        void AddCollection<T>(ref Collection<T> list, IEnumerable enumerable)
            where T : class
        {
            if (enumerable != null)
            {
                foreach (object obj in enumerable)
                {
                    if (obj != null && obj is T)
                    {
                        Add<T>(ref list, (T)obj);
                    }
                }
            }
        }

        void Add<T>(ref Collection<T> list, T data)
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

        bool GetIsArgumentRequired(PropertyDescriptor propertyDescriptor)
        {
            return propertyDescriptor.Attributes[typeof(RequiredArgumentAttribute)] != null;
        }

        List<string> GetOverloadGroupNames(PropertyDescriptor propertyDescriptor)
        {
            List<string> overloadGroupNames = new List<string>(0);
            AttributeCollection propertyAttributes = propertyDescriptor.Attributes;
            for (int i = 0; i < propertyAttributes.Count; i++)
            {
                Attribute attribute = propertyAttributes[i];
                if (ReflectedInformation.OverloadGroupAttributeType.IsAssignableFrom(attribute.GetType()))
                {
                    overloadGroupNames.Add(((OverloadGroupAttribute)attribute).GroupName);
                }
            }
            return overloadGroupNames;
        }

        [Flags]
        enum ReflectedType
        {
            Argument = 0X1,
            Variable = 0X2,
            Child = 0X4,
            ActivityDelegate = 0X8,
            All = 0XF
        }

        class DictionaryArgumentHelper
        {
            protected DictionaryArgumentHelper()
            {
            }

            public IList<RuntimeArgument> RuntimeArguments
            {
                get;
                protected set;
            }

            public static IList<RuntimeArgument> TryGetRuntimeArguments(object propertyValue, string propertyName)
            {
                // special case each of the non-generic argument types to avoid reflection costs

                IEnumerable<KeyValuePair<string, Argument>> argumentEnumerable = propertyValue as IEnumerable<KeyValuePair<string, Argument>>;
                if (argumentEnumerable != null)
                {
                    return GetRuntimeArguments(argumentEnumerable, propertyName);
                }

                IEnumerable<KeyValuePair<string, InArgument>> inArgumentEnumerable = propertyValue as IEnumerable<KeyValuePair<string, InArgument>>;
                if (inArgumentEnumerable != null)
                {
                    return GetRuntimeArguments(inArgumentEnumerable, propertyName);
                }

                IEnumerable<KeyValuePair<string, OutArgument>> outArgumentEnumerable = propertyValue as IEnumerable<KeyValuePair<string, OutArgument>>;
                if (outArgumentEnumerable != null)
                {
                    return GetRuntimeArguments(outArgumentEnumerable, propertyName);
                }

                IEnumerable<KeyValuePair<string, InOutArgument>> inOutArgumentEnumerable = propertyValue as IEnumerable<KeyValuePair<string, InOutArgument>>;
                if (inOutArgumentEnumerable != null)
                {
                    return GetRuntimeArguments(inOutArgumentEnumerable, propertyName);
                }

                return null;
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
                        string argName = (key == null) ? "<null>" : key;
                        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.MissingArgument(argName, propertyName)));
                    }
                    if (string.IsNullOrEmpty(key))
                    {
                        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.MissingNameProperty(value.ArgumentType)));
                    }

                    RuntimeArgument runtimeArgument = new RuntimeArgument(key, value.ArgumentType, value.Direction, false, null, value);
                    runtimeArguments.Add(runtimeArgument);
                }

                return runtimeArguments;
            }
        }

        class DictionaryArgumentHelper<T> : DictionaryArgumentHelper where T : Argument
        {
            public DictionaryArgumentHelper(object propertyValue, string propertyName)
                : base()
            {
                IEnumerable<KeyValuePair<string, T>> argumentDictionary = propertyValue as IEnumerable<KeyValuePair<string, T>>;

                this.RuntimeArguments = GetRuntimeArguments(argumentDictionary, propertyName);
            }
        }

    }
}
