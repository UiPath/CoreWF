// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;

namespace Microsoft.CoreWf.Expressions
{
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords,
    //Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [New])")]
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix,
    //Justification = "Optimizing for XAML naming.")]
    //[ContentProperty("Arguments")]
    public sealed class New<TResult> : CodeActivity<TResult>
    {
        private Collection<Argument> _arguments;
        private Func<object[], TResult> _function;
        private ConstructorInfo _constructorInfo;
        private static MruCache<ConstructorInfo, Func<object[], TResult>> s_funcCache =
            new MruCache<ConstructorInfo, Func<object[], TResult>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static ReaderWriterLockSlim s_locker = new ReaderWriterLockSlim();

        //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.PropertyNamesShouldNotMatchGetMethods,
        //Justification = "Optimizing for XAML naming.")]
        public Collection<Argument> Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    _arguments = new ValidatingCollection<Argument>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _arguments;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            bool foundError = false;
            ConstructorInfo oldConstructorInfo = _constructorInfo;

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
                    RuntimeArgument runtimeArgument = new RuntimeArgument("Argument" + i, _arguments[i].ArgumentType, _arguments[i].Direction, true);
                    metadata.Bind(_arguments[i], runtimeArgument);
                    metadata.AddArgument(runtimeArgument);
                    types[i] = this.Arguments[i].Direction == ArgumentDirection.In ? this.Arguments[i].ArgumentType : this.Arguments[i].ArgumentType.MakeByRefType();
                }
            }

            // If we didn't find any errors in the arguments then
            // we can look for an appropriate constructor.
            if (!foundError)
            {
                _constructorInfo = typeof(TResult).GetConstructor(types);
                if (_constructorInfo == null && (!typeof(TResult).GetTypeInfo().IsValueType || types.Length > 0))
                {
                    metadata.AddValidationError(SR.ConstructorInfoNotFound(typeof(TResult).Name));
                }
                else if ((_constructorInfo != oldConstructorInfo) || (_function == null))
                {
                    _function = MethodCallExpressionHelper.GetFunc<TResult>(metadata, _constructorInfo, s_funcCache, s_locker);
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
            TResult result = _function(objects);

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
