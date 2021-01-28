namespace Microsoft.CSharp.Activities
{
    using Microsoft.Common;
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Expressions;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Collections;
    using System.Security;
    using System.Threading;

    class CSharpHelper : LanguageHelper
    {
        internal static string Language
        {
            get
            {
                return "C#";
            }
        }

        // the following assemblies are provided to the compiler by default
        // items are public so the decompiler knows which assemblies it doesn't need to reference for interfaces
        public static readonly IReadOnlyCollection<Assembly> DefaultReferencedAssemblies = new HashSet<Assembly>
            {
                typeof(int).Assembly, // mscorlib
                typeof(CodeTypeDeclaration).Assembly, // System
                typeof(Expression).Assembly,             // System.Core
                typeof(Activity).Assembly  // System.Activities
            };

        // cache for type's all base types, interfaces, generic arguments, element type
        // HopperCache is a psuedo-MRU cache
        const int typeReferenceCacheMaxSize = 100;
        static readonly object typeReferenceCacheLock = new object();
        static readonly HopperCache typeReferenceCache = new HopperCache(typeReferenceCacheMaxSize, false);
        static ulong lastTimestamp = 0;

        // Cache<(expressionText+ReturnType+Assemblies+Imports), LambdaExpression>
        // LambdaExpression represents raw ExpressionTrees right out of the C# hosted compiler
        // these raw trees are yet to be rewritten with appropriate Variables
        const int rawTreeCacheMaxSize = 128;
        static readonly object rawTreeCacheLock = new object();
        [Fx.Tag.SecurityNote(Critical = "Critical because it caches objects created under a demand for FullTrust.")]
        [SecurityCritical]
        static HopperCache rawTreeCache;

        static HopperCache RawTreeCache
        {
            [Fx.Tag.SecurityNote(Critical = "Critical because it access critical member rawTreeCache.")]
            [SecurityCritical]
            get
            {
                if (rawTreeCache == null)
                {
                    rawTreeCache = new HopperCache(rawTreeCacheMaxSize, false);
                }
                return rawTreeCache;
            }
        }

        const int HostedCompilerCacheSize = 10;
        [Fx.Tag.SecurityNote(Critical = "Critical because it holds HostedCompilerWrappers which hold HostedCompiler instances, which require FullTrust.")]
        [SecurityCritical]
        static Dictionary<HashSet<Assembly>, HostedCompilerWrapper> HostedCompilerCache;

        [Fx.Tag.SecurityNote(Critical = "Critical because it creates Microsoft.Compiler.CSharp.HostedCompiler, which is in a non-APTCA assembly, and thus has a LinkDemand.",
            Safe = "Safe because it puts the HostedCompiler instance into the HostedCompilerCache member, which is SecurityCritical and we are demanding FullTrust.")]
        [SecuritySafeCritical]
        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static HostedCompilerWrapper GetCachedHostedCompiler(HashSet<Assembly> assemblySet)
        {
            if (HostedCompilerCache == null)
            {
                // we don't want to newup a Dictionary everytime GetCachedHostedCompiler is called only to find out the cache is already initialized.
                Interlocked.CompareExchange(ref HostedCompilerCache,
                    new Dictionary<HashSet<Assembly>, HostedCompilerWrapper>(HostedCompilerCacheSize, HashSet<Assembly>.CreateSetComparer()),
                    null);
            }

            lock (HostedCompilerCache)
            {
                HostedCompilerWrapper hcompilerWrapper;
                if (HostedCompilerCache.TryGetValue(assemblySet, out hcompilerWrapper))
                {
                    hcompilerWrapper.Reserve(unchecked(++CSharpHelper.lastTimestamp));
                    return hcompilerWrapper;
                }

                if (HostedCompilerCache.Count >= HostedCompilerCacheSize)
                {
                    // Find oldest used compiler to kick out
                    ulong oldestTimestamp = ulong.MaxValue;
                    HashSet<Assembly> oldestCompiler = null;
                    foreach (KeyValuePair<HashSet<Assembly>, HostedCompilerWrapper> kvp in HostedCompilerCache)
                    {
                        if (oldestTimestamp > kvp.Value.Timestamp)
                        {
                            oldestCompiler = kvp.Key;
                            oldestTimestamp = kvp.Value.Timestamp;
                        }
                    }

                    if (oldestCompiler != null)
                    {
                        hcompilerWrapper = HostedCompilerCache[oldestCompiler];
                        HostedCompilerCache.Remove(oldestCompiler);
                        hcompilerWrapper.MarkAsKickedOut();
                    }
                }

                hcompilerWrapper = new HostedCompilerWrapper(new CSharpJustInTimeCompiler(assemblySet));
                HostedCompilerCache[assemblySet] = hcompilerWrapper;
                hcompilerWrapper.Reserve(unchecked(++CSharpHelper.lastTimestamp));

                return hcompilerWrapper;
            }
        }

        public CSharpHelper(string expressionText, HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames)
            : this(expressionText)
        {
            Initialize(refAssemNames, namespaceImportsNames);
        }

        CSharpHelper(string expressionText) : base(expressionText)
        {
        }

        public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            List<string> localNamespaces;
            List<AssemblyReference> localAssemblies;
            GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity,
                false, out localNamespaces, out localAssemblies);

            CSharpHelper helper = new CSharpHelper(expressionText);
            HashSet<AssemblyName> localReferenceAssemblies = new HashSet<AssemblyName>();
            HashSet<string> localImports = new HashSet<string>(localNamespaces);
            foreach (AssemblyReference assemblyReference in localAssemblies)
            {
                if (assemblyReference.Assembly != null)
                {
                    // directly add the Assembly to the list
                    // so that we don't have to go through 
                    // the assembly resolution process
                    if (helper.referencedAssemblies == null)
                    {
                        helper.referencedAssemblies = new HashSet<Assembly>();
                    }
                    helper.referencedAssemblies.Add(assemblyReference.Assembly);
                }
                else if (assemblyReference.AssemblyName != null)
                {
                    localReferenceAssemblies.Add(assemblyReference.AssemblyName);
                }
            }

            helper.Initialize(localReferenceAssemblies, localImports);
            return helper.Compile<T>(publicAccessor, isLocationExpression);
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because it invokes a HostedCompiler, which requires FullTrust and also accesses RawTreeCache, which is SecurityCritical.",
            Safe = "Safe because we are demanding FullTrust.")]
        [SecuritySafeCritical]
        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public LambdaExpression CompileNonGeneric(LocationReferenceEnvironment environment)
        {
            bool abort;
            Expression finalBody;
            this.environment = environment;
            if (referencedAssemblies == null)
            {
                referencedAssemblies = new HashSet<Assembly>();
            }
            referencedAssemblies.UnionWith(DefaultReferencedAssemblies);

            RawTreeCacheKey rawTreeKey = new RawTreeCacheKey(
                TextToCompile,
                null,
                referencedAssemblies,
                namespaceImports);

            RawTreeCacheValueWrapper rawTreeHolder = RawTreeCache.GetValue(rawTreeCacheLock, rawTreeKey) as RawTreeCacheValueWrapper;
            if (rawTreeHolder != null)
            {
                // try short-cut
                // if variable resolution fails at Rewrite, rewind and perform normal compile steps
                LambdaExpression rawTree = rawTreeHolder.Value;
                isShortCutRewrite = true;
                finalBody = Rewrite(rawTree.Body, null, false, out abort);
                isShortCutRewrite = false;

                if (!abort)
                {
                    return Expression.Lambda(rawTree.Type, finalBody, rawTree.Parameters);
                }

                // if we are here, then that means the shortcut Rewrite failed.
                // we don't want to see too many of C# expressions in this pass since we just wasted Rewrite time for no good.
            }

            var scriptAndTypeScope = new CSharpScriptAndTypeScope(environment);

            var compilerWrapper = GetCachedHostedCompiler(referencedAssemblies);
            var compiler = compilerWrapper.Compiler;
            LambdaExpression lambda = null;
            try
            {
                lock (compiler)
                {
                    try
                    {
                        lambda = compiler.CompileExpression(ExpressionToCompile(scriptAndTypeScope.FindVariable, typeof(object)));
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                        FxTrace.Exception.TraceUnhandledException(e);
                        throw;
                    }
                }
            }
            finally
            {
                compilerWrapper.Release();
            }

            if (scriptAndTypeScope.ErrorMessage != null)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(TextToCompile, scriptAndTypeScope.ErrorMessage)));
            }

            // replace the field references with variable references to our dummy variables
            // and rewrite lambda.body.Type to equal the lambda return type T            
            if (lambda == null)
            {
                // ExpressionText was either an empty string or Null
                // we return null which eventually evaluates to default(TResult) at execution time.
                return null;
            }
            // add the pre-rewrite lambda to RawTreeCache
            var typedLambda = lambda.Body is UnaryExpression ? Expression.Lambda(((UnaryExpression)lambda.Body).Operand, lambda.Parameters) : lambda;

            AddToRawTreeCache(rawTreeKey, rawTreeHolder, typedLambda);

            finalBody = Rewrite(typedLambda.Body, null, false, out abort);
            Fx.Assert(abort == false, "this non-shortcut Rewrite must always return abort == false");

            return Expression.Lambda(finalBody, lambda.Parameters);
        }

        public Expression<Func<ActivityContext, T>> Compile<T>(CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationReference = false)
        {
            this.publicAccessor = publicAccessor;

            return Compile<T>(publicAccessor.ActivityMetadata.Environment, isLocationReference);
        }

        // Soft-Link: This method is called through reflection by CSharpDesignerHelper.
        public Expression<Func<ActivityContext, T>> Compile<T>(LocationReferenceEnvironment environment)
        {
            Fx.Assert(publicAccessor == null, "No public accessor so the value for isLocationReference doesn't matter");
            return Compile<T>(environment, false);
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because it invokes a HostedCompiler, which requires FullTrust and also accesses RawTreeCache, which is SecurityCritical.",
            Safe = "Safe because we are demanding FullTrust.")]
        [SecuritySafeCritical]
        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public Expression<Func<ActivityContext, T>> Compile<T>(LocationReferenceEnvironment environment, bool isLocationReference)
        {
            bool abort;
            Expression finalBody;
            Type lambdaReturnType = typeof(T);

            this.environment = environment;
            if (referencedAssemblies == null)
            {
                referencedAssemblies = new HashSet<Assembly>();
            }
            referencedAssemblies.UnionWith(DefaultReferencedAssemblies);

            RawTreeCacheKey rawTreeKey = new RawTreeCacheKey(
                TextToCompile,
                lambdaReturnType,
                referencedAssemblies,
                namespaceImports);

            RawTreeCacheValueWrapper rawTreeHolder = RawTreeCache.GetValue(rawTreeCacheLock, rawTreeKey) as RawTreeCacheValueWrapper;
            if (rawTreeHolder != null)
            {
                // try short-cut
                // if variable resolution fails at Rewrite, rewind and perform normal compile steps
                LambdaExpression rawTree = rawTreeHolder.Value;
                isShortCutRewrite = true;
                finalBody = Rewrite(rawTree.Body, null, isLocationReference, out abort);
                isShortCutRewrite = false;

                if (!abort)
                {
                    Fx.Assert(finalBody.Type == lambdaReturnType, "Compiler generated ExpressionTree return type doesn't match the target return type");
                    // convert it into the our expected lambda format (context => ...)
                    return Expression.Lambda<Func<ActivityContext, T>>(finalBody,
                        FindParameter(finalBody) ?? ExpressionUtilities.RuntimeContextParameter);
                }

                // if we are here, then that means the shortcut Rewrite failed.
                // we don't want to see too many of C# expressions in this pass since we just wasted Rewrite time for no good.

                if (publicAccessor != null)
                {
                    // from the preceding shortcut rewrite, we probably have generated tempAutoGeneratedArguments
                    // they are not valid anymore since we just aborted the shortcut rewrite.
                    // clean up, and start again.

                    publicAccessor.Value.ActivityMetadata.CurrentActivity.ResetTempAutoGeneratedArguments();
                }
            }

            // ensure the return type's assembly is added to ref assembly list
            HashSet<Type> allBaseTypes = null;
            EnsureTypeReferenced(lambdaReturnType, ref allBaseTypes);
            foreach (Type baseType in allBaseTypes)
            {
                // allBaseTypes list always contains lambdaReturnType
                referencedAssemblies.Add(baseType.Assembly);
            }

            var scriptAndTypeScope = new CSharpScriptAndTypeScope(environment);

            var compilerWrapper = GetCachedHostedCompiler(referencedAssemblies);
            var compiler = compilerWrapper.Compiler;

            LambdaExpression lambda = null;
            try
            {
                lock (compiler)
                {
                    try
                    {
                        lambda = compiler.CompileExpression(ExpressionToCompile(scriptAndTypeScope.FindVariable, lambdaReturnType));
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                        // We never want to end up here, Compiler bugs needs to be fixed.
                        FxTrace.Exception.TraceUnhandledException(e);
                        throw;
                    }
                }
            }
            finally
            {
                compilerWrapper.Release();
            }

            if (scriptAndTypeScope.ErrorMessage != null)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(TextToCompile, scriptAndTypeScope.ErrorMessage)));
            }

            // replace the field references with variable references to our dummy variables
            // and rewrite lambda.body.Type to equal the lambda return type T            
            if (lambda == null)
            {
                // ExpressionText was either an empty string or Null
                // we return null which eventually evaluates to default(TResult) at execution time.
                return null;
            }

            // add the pre-rewrite lambda to RawTreeCache
            AddToRawTreeCache(rawTreeKey, rawTreeHolder, lambda);

            finalBody = Rewrite(lambda.Body, null, isLocationReference, out abort);
            Fx.Assert(abort == false, "this non-shortcut Rewrite must always return abort == false");
            Fx.Assert(finalBody.Type == lambdaReturnType, "Compiler generated ExpressionTree return type doesn't match the target return type");

            // convert it into the our expected lambda format (context => ...)
            return Expression.Lambda<Func<ActivityContext, T>>(finalBody,
                FindParameter(finalBody) ?? ExpressionUtilities.RuntimeContextParameter);
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because it access SecurityCritical member RawTreeCache, thus requiring FullTrust.",
            Safe = "Safe because we are demanding FullTrust.")]
        [SecuritySafeCritical]
        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void AddToRawTreeCache(RawTreeCacheKey rawTreeKey, RawTreeCacheValueWrapper rawTreeHolder, LambdaExpression lambda)
        {
            if (rawTreeHolder != null)
            {
                // this indicates that the key had been found in RawTreeCache,
                // but the value Expression Tree failed the short-cut Rewrite.
                // ---- is really not an issue here, because
                // any one of possibly many raw Expression Trees that are all 
                // represented by the same key can be written here.
                rawTreeHolder.Value = lambda;
            }
            else
            {
                // we never hit RawTreeCache with the given key
                lock (rawTreeCacheLock)
                {
                    // ensure we don't add the same key with two differnt RawTreeValueWrappers
                    if (RawTreeCache.GetValue(rawTreeCacheLock, rawTreeKey) == null)
                    {
                        // do we need defense against alternating miss of the shortcut Rewrite?
                        RawTreeCache.Add(rawTreeKey, new RawTreeCacheValueWrapper() { Value = lambda });
                    }
                }
            }
        }

        static void EnsureTypeReferenced(Type type, ref HashSet<Type> typeReferences)
        {
            // lookup cache 
            // underlying assumption is that type's inheritance(or interface) hierarchy 
            // stays static throughout the lifetime of AppDomain
            HashSet<Type> alreadyVisited = (HashSet<Type>)typeReferenceCache.GetValue(typeReferenceCacheLock, type);
            if (alreadyVisited != null)
            {
                if (typeReferences == null)
                {
                    // used in CSharpHelper.Compile<>
                    // must not alter this set being returned for integrity of cache
                    typeReferences = alreadyVisited;
                }
                else
                {
                    // used in CSharpDesignerHelper.FindTypeReferences
                    typeReferences.UnionWith(alreadyVisited);
                }
                return;
            }

            alreadyVisited = new HashSet<Type>();
            EnsureTypeReferencedRecurse(type, alreadyVisited);

            // cache resulting alreadyVisited set for fast future lookup
            lock (typeReferenceCacheLock)
            {
                typeReferenceCache.Add(type, alreadyVisited);
            }

            if (typeReferences == null)
            {
                // used in CSharpHelper.Compile<>
                // must not alter this set being returned for integrity of cache
                typeReferences = alreadyVisited;
            }
            else
            {
                // used in CSharpDesignerHelper.FindTypeReferences
                typeReferences.UnionWith(alreadyVisited);
            }
            return;
        }

        class CSharpScriptAndTypeScope
        {
            readonly LocationReferenceEnvironment environmentProvider;

            public CSharpScriptAndTypeScope(LocationReferenceEnvironment environmentProvider)
            {
                this.environmentProvider = environmentProvider;
            }

            public string ErrorMessage { get; private set; }

            public Type FindVariable(string name)
            {
                LocationReference referenceToReturn = null;
                FindMatch findMatch = delegateFindAllLocationReferenceMatch;
                bool foundMultiple;
                referenceToReturn = FindLocationReferencesFromEnvironment(environmentProvider, findMatch, name, null, out foundMultiple);
                if (referenceToReturn != null)
                {
                    if (foundMultiple)
                    {
                        // we have duplicate variable names in the same visible environment!!!!
                        // compile error here!!!!
                        ErrorMessage = SR.AmbiguousCSVariableReference(name);
                        return null;
                    }
                    else
                    {
                        return referenceToReturn.Type;
                    }
                }
                return null;
            }
        }
    }
}