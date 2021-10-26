// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xaml;
    using System.Xml;
    using System.Security;
    //using System.Xaml.Permissions;
    using System.Activities.Expressions;
    using System.Activities.Validation;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Text;

    public static class ActivityXamlServices
    {
        private static readonly XamlSchemaContext dynamicActivityReaderSchemaContext = new DynamicActivityReaderSchemaContext();

        public static ActivityBuilder LoadActivityBuilder(Stream stream, XamlObjectWriterSettings xamlObjectWriterSettings = null)
        {
            // System.Activities must be loaded before the XamlSchemaContext constructor
            typeof(Activity).GetHashCode();
            var xamlReader = CreateBuilderReader(new XamlXmlReader(stream));
            XamlObjectWriter objectWriter = new(xamlReader.SchemaContext, xamlObjectWriterSettings);
            XamlServices.Transform(xamlReader, objectWriter);
            return (ActivityBuilder)objectWriter.Result;
        }

        public static Activity Load(Stream stream)
        {
            if (stream == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(stream));
            }

            return Load(stream, new ActivityXamlServicesSettings());
        }

        public static Activity Load(Stream stream, ActivityXamlServicesSettings settings)
        {
            if (stream == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(stream));
            }

            if (settings == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(settings));
            }

            using (XmlReader xmlReader = XmlReader.Create(stream))
            {
                return Load(xmlReader, settings);
            }
        }

        public static Activity Load(string fileName)
        {
            if (fileName == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(fileName));
            }

            return Load(fileName, new ActivityXamlServicesSettings());
        }
        
        public static Activity Load(string fileName, ActivityXamlServicesSettings settings)
        {
            if (fileName == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(fileName));
            }

            if (settings == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(settings));
            }

            using (XmlReader xmlReader = XmlReader.Create(fileName))
            {
                return Load(xmlReader, settings);
            }
        }

        public static Activity Load(TextReader textReader)
        {
            if (textReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(textReader));
            }

            return Load(textReader, new ActivityXamlServicesSettings());
        }

        public static Activity Load(TextReader textReader, ActivityXamlServicesSettings settings)
        {
            if (textReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(textReader));
            }

            if (settings == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(settings));
            }

            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                return Load(xmlReader, settings);
            }
        }

        public static Activity Load(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(xmlReader));
            }

            return Load(xmlReader, new ActivityXamlServicesSettings());
        }

        public static Activity Load(XmlReader xmlReader, ActivityXamlServicesSettings settings)
        {
            if (xmlReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(xmlReader));
            }
            
            if (settings == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(settings));
            }

            using (XamlXmlReader xamlReader = new XamlXmlReader(xmlReader, dynamicActivityReaderSchemaContext))
            {
                return Load(xamlReader, settings);
            }
        }

        public static Activity Load(XamlReader xamlReader)
        {
            if (xamlReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(xamlReader));
            }

            return Load(xamlReader, new ActivityXamlServicesSettings());
        }

        public static Activity Load(XamlReader xamlReader, ActivityXamlServicesSettings settings)
        {
            if (xamlReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(xamlReader));
            }

            if (settings == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(settings));
            }

            DynamicActivityXamlReader dynamicActivityReader = new DynamicActivityXamlReader(xamlReader);
            object xamlObject = XamlServices.Load(dynamicActivityReader);
            if (!(xamlObject is Activity result))
            {
                throw FxTrace.Exception.Argument("reader", SR.ActivityXamlServicesRequiresActivity(
                    xamlObject != null ? xamlObject.GetType().FullName : string.Empty));
            }

            if (result is IDynamicActivity dynamicActivity && settings.CompileExpressions)
            {
                Compile(dynamicActivity, settings);
            }

            return result;
        }

        public static XamlReader CreateReader(Stream stream)
        {
            if (stream == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(stream));
            }

            return CreateReader(new XamlXmlReader(XmlReader.Create(stream), dynamicActivityReaderSchemaContext), dynamicActivityReaderSchemaContext);
        }

        public static XamlReader CreateReader(XamlReader innerReader)
        {
            if (innerReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
            }

            return new DynamicActivityXamlReader(innerReader);
        }

        public static XamlReader CreateReader(XamlReader innerReader, XamlSchemaContext schemaContext)
        {
            if (innerReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
            }

            if (schemaContext == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(schemaContext));
            }

            return new DynamicActivityXamlReader(innerReader, schemaContext);
        }

        public static XamlReader CreateBuilderReader(XamlReader innerReader)
        {
            if (innerReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
            }

            return new DynamicActivityXamlReader(true, innerReader, null);
        }

        public static XamlReader CreateBuilderReader(XamlReader innerReader, XamlSchemaContext schemaContext)
        {
            if (innerReader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(innerReader));
            }

            if (schemaContext == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(schemaContext));
            }

            return new DynamicActivityXamlReader(true, innerReader, schemaContext);
        }

        public static XamlWriter CreateBuilderWriter(XamlWriter innerWriter)
        {
            if (innerWriter == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(innerWriter));
            }

            return new ActivityBuilderXamlWriter(innerWriter);
        }

        public static Func<object> CreateFactory(XamlReader reader, Type resultType)
        {
            if (reader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(reader));
            }
            if (resultType == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(resultType));
            }
            return FuncFactory.CreateFunc(reader, resultType);
        }

        public static Func<T> CreateFactory<T>(XamlReader reader) where T : class
        {
            if (reader == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(reader));
            }
            return FuncFactory.CreateFunc<T>(reader);
        }

        private static void Compile(IDynamicActivity dynamicActivity, ActivityXamlServicesSettings settings)
        {
            string language = null;
            if (!RequiresCompilation(dynamicActivity, settings.LocationReferenceEnvironment, out language))
            {
                return;
            }
            var aotCompiler = settings.GetCompiler(language);
            var compiler = new TextExpressionCompiler(GetCompilerSettings(dynamicActivity, language, aotCompiler));
            var results = compiler.Compile();

            if (results.HasErrors)
            {
                var messages = new StringBuilder();
                foreach (TextExpressionCompilerError message in results.CompilerMessages)
                {
                    messages.Append("\r\n");
                    messages.Append(string.Concat(" ", SR.ActivityXamlServiceLineString, " ", message.SourceLineNumber, ": "));
                    messages.Append(message.Message);

                }
                messages.Append("\r\n");

                InvalidOperationException exception = new InvalidOperationException(SR.ActivityXamlServicesCompilationFailed(messages.ToString()));

                foreach (TextExpressionCompilerError message in results.CompilerMessages)
                {
                    exception.Data.Add(message, message.Message);
                }
                throw FxTrace.Exception.AsError(exception);
            }

            Type compiledExpressionRootType = results.ResultType;

            ICompiledExpressionRoot compiledExpressionRoot = Activator.CreateInstance(compiledExpressionRootType, new object[] { dynamicActivity }) as ICompiledExpressionRoot;
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(dynamicActivity, compiledExpressionRoot);
        }

        private static bool RequiresCompilation(IDynamicActivity dynamicActivity, LocationReferenceEnvironment environment, out string language)
        {
            language = null;

            if (!((Activity)dynamicActivity).IsMetadataCached)
            {
                IList<ValidationError> validationErrors = null;
                if (environment == null)
                {
                    environment = new ActivityLocationReferenceEnvironment { CompileExpressions = true };
                }

                try
                {
                    ActivityUtilities.CacheRootMetadata((Activity)dynamicActivity, environment, ProcessActivityTreeOptions.FullCachingOptions, null, ref validationErrors);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CompiledExpressionsCacheMetadataException(dynamicActivity.Name, e.ToString())));
                }

            }

            DynamicActivityVisitor vistor = new DynamicActivityVisitor();
            vistor.Visit((Activity)dynamicActivity, true);

            if (!vistor.RequiresCompilation)
            {
                return false;
            }
            if (vistor.HasLanguageConflict)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DynamicActivityMultipleExpressionLanguages(vistor.GetConflictingLanguages().AsCommaSeparatedValues())));
            }
            language = vistor.Language;
            return true;
        }

        private static TextExpressionCompilerSettings GetCompilerSettings(IDynamicActivity dynamicActivity, string language, AheadOfTimeCompiler compiler)
        {
            var activity = (Activity)dynamicActivity;
            var name = dynamicActivity.Name ?? activity.DisplayName;
            int lastIndexOfDot = name.LastIndexOf('.');
            int lengthOfName = name.Length;

            string activityName = lastIndexOfDot > 0 ? name.Substring(lastIndexOfDot + 1) : name;
            activityName += "_CompiledExpressionRoot";
            string activityNamespace = lastIndexOfDot > 0 ? name.Substring(0, lastIndexOfDot) : null;

            return new TextExpressionCompilerSettings()
            {
                Activity = activity,
                ActivityName = activityName,
                ActivityNamespace = activityNamespace,
                RootNamespace = null,
                GenerateAsPartialClass = false,
                AlwaysGenerateSource = true,
                Language = language,
                Compiler = compiler
            };
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because we use SecurityCritical methods that do Asserts.",
            Safe = "Safe because no critical resources are leaked. And we guarantee that the XAML we are accessing is coming from the assembly to which we are asserting access.")]
        [SecuritySafeCritical]
        public static void InitializeComponent(
            Type componentType,
            Object componentInstance
        )
        {
            if (componentType == null)
            {
                throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(componentType)));
            }

            if (componentInstance == null)
            {
                throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(componentInstance)));
            }

            Assembly typesAssembly = componentType.Assembly;

            // Get the set of resources from the type's assembly.
            string typeName = componentType.Name;
            string typeNamespace = componentType.Namespace;
            string[] resources = typesAssembly.GetManifestResourceNames();

            // Look for the special resource that is generated by the BeforeInitializeComponentExtension.
            string beforeInitializeResourceName;
            if (string.IsNullOrWhiteSpace(typeNamespace))
            {
                beforeInitializeResourceName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.{2}", typeName, "BeforeInitializeComponentHelper", "txt");
            }
            else
            {
                beforeInitializeResourceName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}.{3}", typeNamespace, typeName, "BeforeInitializeComponentHelper", "txt");
            }

            string beforeInitializeResource = FindResource(resources, beforeInitializeResourceName);
            if (beforeInitializeResource == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BeforeInitializeComponentXBTExtensionResourceNotFound));
            }
            GetContentsOfBeforeInitializeExtensionResource(typesAssembly, beforeInitializeResource, out string xamlResourceName, out string helperClassName);

            // Now look for the resource containing the XAML.
            string fullXamlResourceName = FindResource(resources, xamlResourceName);
            if (fullXamlResourceName == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.XamlBuildTaskResourceNotFound(xamlResourceName)));
            }

            // Get the schema context for the type.
            XamlSchemaContext typeSchemaContext = GetXamlSchemaContext(typesAssembly, helperClassName);

            InitializeComponentFromXamlResource(componentType, fullXamlResourceName, componentInstance, typeSchemaContext);
        }

        private static string FindResource(string[] resources, string partialResourceName)
        {
            bool foundResourceString = false;
            int resourceIndex;
            for (resourceIndex = 0; (resourceIndex < resources.Length); resourceIndex = (resourceIndex + 1))
            {
                string resource = resources[resourceIndex];
                if ((resource.Contains("." + partialResourceName) || resource.Equals(partialResourceName)))
                {
                    foundResourceString = true;
                    break;
                }
            }
            if (!foundResourceString)
            {
                return null;
            }
            return resources[resourceIndex];
        }

        private static void GetContentsOfBeforeInitializeExtensionResource(Assembly assembly, string resource, out string xamlResourceName, out string helperClassName)
        {
            Stream beforeInitializeStream = assembly.GetManifestResourceStream(resource);
            using (StreamReader beforeInitializeReader = new StreamReader(beforeInitializeStream))
            {
                xamlResourceName = beforeInitializeReader.ReadLine();
                helperClassName = beforeInitializeReader.ReadLine();
            }
        }

        //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureAsserts,
        //    Justification = "The schema context is not critical data because it is exposed through the assembly manifest and we are asserting to go get that data.")]
        [Fx.Tag.SecurityNote(Critical = "Critical because it Asserts ReflectionPermission(MemberAccess) to the calling assembly.")]
        [SecurityCritical]
        private static XamlSchemaContext GetXamlSchemaContext(Assembly assembly, string helperClassName)
        {
            XamlSchemaContext typeSchemaContext = null;
#if NET45
            ReflectionPermission reflectionPerm = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
            reflectionPerm.Assert();
#endif
            try
            {
                Type schemaContextType = assembly.GetType(helperClassName);
                if (schemaContextType == null)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SchemaContextFromBeforeInitializeComponentXBTExtensionNotFound(helperClassName)));
                }

                // The "official" BeforeInitializeComponent XBT Extension will not create a generic type for this helper class.
                // This check is here so that the assembly manifest can't lure us into creating a type with a generic argument from a different assembly.
                if (schemaContextType.IsGenericType || schemaContextType.IsGenericTypeDefinition)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SchemaContextFromBeforeInitializeComponentXBTExtensionCannotBeGeneric(helperClassName)));
                }

                PropertyInfo schemaContextPropertyInfo = schemaContextType.GetProperty("SchemaContext",
                    BindingFlags.NonPublic | BindingFlags.Static);
                typeSchemaContext = (XamlSchemaContext)schemaContextPropertyInfo.GetValue(null,
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty, null, null, null);
            }
            finally
            {
#if NET45
                CodeAccessPermission.RevertAssert();
#endif
            }
            return typeSchemaContext;
        }

        //[SuppressMessage(FxCop.Category.Security, "CA2103:ReviewImperativeSecurity",
        //    Justification = "Passing XamlAccessLevel to XamlLoadPermission is okay.")]
        //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureAsserts,
        //    Justification = "We are asserting to get private access to the componentType only so that we can initialize it.")]
        [Fx.Tag.SecurityNote(Critical = "Critical because it Asserts XamlLoadPermission(XamlAccessLevel.PrivateAccessTo(type).")]
        [SecurityCritical]
        private static void InitializeComponentFromXamlResource(Type componentType, string resource, object componentInstance, XamlSchemaContext schemaContext)
        {
            Stream initializeXaml = componentType.Assembly.GetManifestResourceStream(resource);
            XmlReader xmlReader = null;
            XamlReader reader = null;
            XamlObjectWriter objectWriter = null;
            try
            {
                xmlReader = XmlReader.Create(initializeXaml);
                XamlXmlReaderSettings readerSettings = new XamlXmlReaderSettings
                {
                    LocalAssembly = componentType.Assembly,
                    AllowProtectedMembersOnRoot = true
                };
                reader = new XamlXmlReader(xmlReader, schemaContext, readerSettings);
                XamlObjectWriterSettings writerSettings = new XamlObjectWriterSettings
                {
                    RootObjectInstance = componentInstance
                };
                //writerSettings.AccessLevel = XamlAccessLevel.PrivateAccessTo(componentType);
                objectWriter = new XamlObjectWriter(schemaContext, writerSettings);

                // We need the XamlLoadPermission for the assembly we are dealing with.
                //XamlLoadPermission perm = new XamlLoadPermission(XamlAccessLevel.PrivateAccessTo(componentType));
                //perm.Assert();
                try
                {
                    XamlServices.Transform(reader, objectWriter);
                }
                finally
                {
                    //CodeAccessPermission.RevertAssert();
                }
            }
            finally
            {
                if ((xmlReader != null))
                {
                    ((IDisposable)(xmlReader)).Dispose();
                }
                if ((reader != null))
                {
                    ((IDisposable)(reader)).Dispose();
                }
                if ((objectWriter != null))
                {
                    ((IDisposable)(objectWriter)).Dispose();
                }
            }
        }

        private class DynamicActivityReaderSchemaContext : XamlSchemaContext
        {
            private static bool serviceModelLoaded;
            private const string serviceModelDll = "System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            private const string serviceModelActivitiesDll = "System.ServiceModel.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            private const string serviceModelNamespace = "http://schemas.microsoft.com/netfx/2009/xaml/servicemodel";

            // Eventually this will be unnecessary since XAML team has changed the default behavior
            public DynamicActivityReaderSchemaContext()
                : base(new XamlSchemaContextSettings())
            {
            }

            protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
            {
                XamlType xamlType = base.GetXamlType(xamlNamespace, name, typeArguments);

                if (xamlType == null)
                {
                    if (xamlNamespace == serviceModelNamespace && !serviceModelLoaded)
                    {
                        Assembly.Load(serviceModelDll);
                        Assembly.Load(serviceModelActivitiesDll);
                        serviceModelLoaded = true;
                        xamlType = base.GetXamlType(xamlNamespace, name, typeArguments);
                    }                        
                }
                return xamlType;
            }
        }

        private class DynamicActivityVisitor : CompiledExpressionActivityVisitor
        {
            private ISet<string> languages;

            public string Language
            {
                get
                {
                    if (this.languages == null || this.languages.Count == 0 || this.languages.Count > 1)
                    {
                        return null;
                    }

                    IEnumerator<string> languagesEnumerator = this.languages.GetEnumerator();

                    if (languagesEnumerator.MoveNext())
                    {
                        return languagesEnumerator.Current;
                    }

                    return null;
                }
            }

            public bool RequiresCompilation
            {
                get;
                private set;
            }

            public bool HasLanguageConflict
            {
                get
                {
                    return this.languages != null && this.languages.Count > 1;
                }
            }

            public IEnumerable<string> GetConflictingLanguages()
            {
                if (this.languages.Count > 1)
                {
                    return this.languages;
                }
                else
                {
                    return null;
                }
            }

            protected override void VisitITextExpression(Activity activity, out bool exit)
            {

                if (activity is ITextExpression textExpression)
                {
                    if (textExpression.RequiresCompilation)
                    {
                        this.RequiresCompilation = true;

                        if (this.languages == null)
                        {
                            this.languages = new HashSet<string>();
                        }

                        if (!this.languages.Contains(textExpression.Language))
                        {
                            this.languages.Add(textExpression.Language);
                        }
                    }
                }

                base.VisitITextExpression(activity, out exit);
            }
        }
    }
}
