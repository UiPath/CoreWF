// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using CoreWf;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using CoreWf.Runtime.Collections;
    using Portable.Xaml.Markup;
    using System.Threading;
    using System;
    using CoreWf.Runtime;
    using CoreWf.Internals;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords,
    //    Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [New])")]
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix,
    //    Justification = "Optimizing for XAML naming.")]
    [ContentProperty("Arguments")]
    public sealed class New<TResult> : CodeActivity<TResult> 
    {
        private Collection<Argument> arguments;
        private Func<object[], TResult> function;
        private ConstructorInfo constructorInfo;
        private static readonly MruCache<ConstructorInfo, Func<object[], TResult>> funcCache = 
            new MruCache<ConstructorInfo, Func<object[], TResult>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.PropertyNamesShouldNotMatchGetMethods,
        //    Justification = "Optimizing for XAML naming.")]
        public Collection<Argument> Arguments
        {
            get
            {
                if (this.arguments == null)
                {
                    this.arguments = new ValidatingCollection<Argument>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw FxTrace.Exception.ArgumentNull(nameof(item));
                            }
                        }
                    };
                }
                return this.arguments;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            bool foundError = false;
            ConstructorInfo oldConstructorInfo = this.constructorInfo;

            // Loop through each argument, validate it, and if validation
            // passed expose it to the metadata
            Type[] types = new Type[this.Arguments.Count];
            for (int i = 0; i < this.Arguments.Count; i++)
            {
                Argument argument = this.Arguments[i];
                if (argument == null || argument.Expression == null)
                {
                    metadata.AddValidationError(SR.ArgumentRequired("Arguments", typeof(New<TResult>)));
                    foundError = true;
                }
                else
                {
                    RuntimeArgument runtimeArgument = new RuntimeArgument("Argument" + i, this.arguments[i].ArgumentType, this.arguments[i].Direction, true);
                    metadata.Bind(this.arguments[i], runtimeArgument);
                    metadata.AddArgument(runtimeArgument);
                    types[i] = this.Arguments[i].Direction == ArgumentDirection.In ? this.Arguments[i].ArgumentType : this.Arguments[i].ArgumentType.MakeByRefType();
                }
            }

            // If we didn't find any errors in the arguments then
            // we can look for an appropriate constructor.
            if (!foundError)
            {
                constructorInfo = typeof(TResult).GetConstructor(types);
                if (constructorInfo == null && (!typeof(TResult).IsValueType || types.Length > 0))
                {
                    metadata.AddValidationError(SR.ConstructorInfoNotFound(typeof(TResult).Name));
                }
                else if ((this.constructorInfo != oldConstructorInfo) || (this.function == null))
                {
                    this.function = MethodCallExpressionHelper.GetFunc<TResult>(metadata, constructorInfo, funcCache, locker);
                }
            }
        } 

        protected override TResult Execute(CodeActivityContext context)
        {
            object[] objects = new object[this.Arguments.Count];
            for (int i = 0; i < this.Arguments.Count; i++)
            {
                objects[i] = this.Arguments[i].Get(context);
            }
            TResult result = this.function(objects);
            
            for (int i = 0; i < this.Arguments.Count; i++)
            {
                Argument argument = this.Arguments[i];
                if (argument.Direction == ArgumentDirection.InOut || argument.Direction == ArgumentDirection.Out)
                {
                    argument.Set(context, objects[i]);
                }
            }
            return result;
        }

    }
}
