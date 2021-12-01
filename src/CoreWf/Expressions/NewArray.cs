// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Markup;

namespace System.Activities.Expressions;

[ContentProperty("Bounds")]
public sealed class NewArray<TResult> : CodeActivity<TResult>
{
    private Collection<Argument> _bounds;
    private ConstructorInfo _constructorInfo;

    public Collection<Argument> Bounds
    {
        get
        {
            _bounds ??= new ValidatingCollection<Argument>
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
        Type[] types = new Type[Bounds.Count];
        for (int i = 0; i < Bounds.Count; i++)
        {
            Argument argument = Bounds[i];
            if (argument == null || argument.IsEmpty)
            {
                metadata.AddValidationError(SR.ArgumentRequired("Bounds", typeof(NewArray<TResult>)));
                foundError = true;
            }
            else
            {
                if (!IsIntegralType(argument.ArgumentType))
                {
                    metadata.AddValidationError(SR.NewArrayBoundsRequiresIntegralArguments);
                    foundError = true;
                }
                else
                {
                    RuntimeArgument runtimeArgument = new("Argument" + i, Bounds[i].ArgumentType, _bounds[i].Direction, true);
                    metadata.Bind(Bounds[i], runtimeArgument);
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
        object[] objects = new object[Bounds.Count];
        int i = 0;
        foreach (Argument argument in Bounds)
        {
            objects[i] = argument.Get(context);
            i++;
        }
        TResult result = (TResult)_constructorInfo.Invoke(objects);
        return result;
    }

    private static bool IsIntegralType(Type type)
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
