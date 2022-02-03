// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;
using XamlReader = System.Xaml.XamlReader;

namespace System.Activities.XamlIntegration;

internal abstract class FuncFactory
{
    internal IList<NamespaceDeclaration> ParentNamespaces { get; set; }

    internal XamlNodeList Nodes { get; init; }

    // Back-compat switch: we don't want to copy parent settings on Activity/DynamicActivity
    internal bool IgnoreParentSettings { get; set; }

    public static Func<object> CreateFunc(XamlReader reader, Type returnType)
    {
        var factory = CreateFactory(null, reader, returnType);
        return factory.GetFunc();
    }

    public static Func<T> CreateFunc<T>(XamlReader reader) where T : class
    {
        var factory = new FuncFactory<T>(null, reader);
        return factory.GetTypedFunc();
    }

    internal abstract Func<object> GetFunc();

    internal static FuncFactory CreateFactory(XamlReader xamlReader, IServiceProvider context)
    {
        var objectWriterFactory = context.GetService(typeof(IXamlObjectWriterFactory)) as IXamlObjectWriterFactory;
        var provideValueService = context.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        Type propertyType = null;
        //
        // IProvideValueTarget.TargetProperty can return DP, Attached Property or MemberInfo for clr property
        // In this case it should always be a regular clr property here - we are always targeting Activity.Body.
        var propertyInfo = provideValueService?.TargetProperty as PropertyInfo;

        if (propertyInfo != null)
        {
            propertyType = propertyInfo.PropertyType;
        }

        var funcFactory = CreateFactory(objectWriterFactory, xamlReader, propertyType?.GetGenericArguments());
        return funcFactory;
    }

    // Back-compat workaround: returnType should only be a single value. But in 4.0 we didn't
    // validate this; we just passed the array in to MakeGenericType, which would throw if there
    // were multiple values. To preserve the same exception, we allow passing in an array here.
    private static FuncFactory CreateFactory(IXamlObjectWriterFactory objectWriterFactory, XamlReader xamlReader,
        params Type[] returnType)
    {
        var closedType = typeof(FuncFactory<>).MakeGenericType(returnType);
        return (FuncFactory) Activator.CreateInstance(closedType, objectWriterFactory, xamlReader);
    }
}

internal class FuncFactory<T> : FuncFactory where T : class
{
    private readonly IXamlObjectWriterFactory _objectWriterFactory;

    public FuncFactory(IXamlObjectWriterFactory objectWriterFactory, XamlReader reader)
    {
        _objectWriterFactory = objectWriterFactory;
        Nodes = new XamlNodeList(reader.SchemaContext);
        XamlServices.Transform(reader, Nodes.Writer);
    }

    internal T Evaluate()
    {
        var writer = GetWriter();
        XamlServices.Transform(Nodes.GetReader(), writer);
        return (T) writer.Result;
    }

    internal override Func<object> GetFunc()
    {
        return (Func<T>) Evaluate;
    }

    internal Func<T> GetTypedFunc()
    {
        return Evaluate;
    }

    private XamlObjectWriter GetWriter() =>
        _objectWriterFactory != null
            ? _objectWriterFactory.GetXamlObjectWriter(GetObjectWriterSettings())
            : new XamlObjectWriter(Nodes.Writer.SchemaContext);

    private XamlObjectWriterSettings GetObjectWriterSettings()
    {
        if (IgnoreParentSettings)
        {
            return new XamlObjectWriterSettings();
        }

        var result = new XamlObjectWriterSettings(_objectWriterFactory.GetParentSettings())
        {
            // The delegate settings are already stripped by XOW. Some other settings don't make sense to copy.
            ExternalNameScope = null,
            RegisterNamesOnExternalNamescope = false,
            RootObjectInstance = null,
            SkipProvideValueOnRoot = false
        };
        return result;
    }
}
