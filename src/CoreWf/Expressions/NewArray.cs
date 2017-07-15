// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CoreWf.Expressions
{
    //[ContentProperty("Bounds")]
    public sealed class NewArray<TResult> : CodeActivity<TResult>
    {
        private Collection<Argument> _bounds;
        private ConstructorInfo _constructorInfo;

        public Collection<Argument> Bounds
        {
            get
            {
                if (_bounds == null)
                {
                    _bounds = new ValidatingCollection<Argument>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _bounds;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (!typeof(TResult).IsArray)
            {
                metadata.AddValidationError(SR.NewArrayRequiresArrayTypeAsResultType);

                // We shortcut any further processing in this case.
                return;
            }

            bool foundError = false;

            // Loop through each argument, validate it, and if validation
            // passed expose it to the metadata
            Type[] types = new Type[this.Bounds.Count];
            for (int i = 0; i < this.Bounds.Count; i++)
            {
                Argument argument = this.Bounds[i];
                if (argument == null || argument.IsEmpty)
                {
                    metadata.AddValidationError(SR.ArgumentRequired("Bounds", typeof(NewArray<TResult>)));
                    foundError = true;
                }
                else
                {
                    if (!isIntegralType(argument.ArgumentType))
                    {
                        metadata.AddValidationError(SR.NewArrayBoundsRequiresIntegralArguments);
                        foundError = true;
                    }
                    else
                    {
                        RuntimeArgument runtimeArgument = new RuntimeArgument("Argument" + i, this.Bounds[i].ArgumentType, _bounds[i].Direction, true);
                        metadata.Bind(this.Bounds[i], runtimeArgument);
                        metadata.AddArgument(runtimeArgument);

                        types[i] = argument.ArgumentType;
                    }
                }
            }

            // If we didn't find any errors in the arguments then
            // we can look for an appropriate constructor.
            if (!foundError)
            {
                _constructorInfo = typeof(TResult).GetConstructor(types);
                if (_constructorInfo == null)
                {
                    metadata.AddValidationError(SR.ConstructorInfoNotFound(typeof(TResult).Name));
                }
            }
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            object[] objects = new object[this.Bounds.Count];
            int i = 0;
            foreach (Argument argument in this.Bounds)
            {
                objects[i] = argument.Get(context);
                i++;
            }
            TResult result = (TResult)_constructorInfo.Invoke(objects);
            return result;
        }

        private bool isIntegralType(Type type)
        {
            if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(char) || type == typeof(short) ||
                type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
