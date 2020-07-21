' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting

    Friend NotInheritable Class VisualBasicScriptCompiler
        Inherits ScriptCompiler

        Public Shared ReadOnly Instance As ScriptCompiler = New VisualBasicScriptCompiler()

        Private Shared ReadOnly s_defaultOptions As VisualBasicParseOptions = New VisualBasicParseOptions(kind:=SourceCodeKind.Script, languageVersion:=LanguageVersion.Latest)
        'Private Shared ReadOnly s_vbRuntimeReference As MetadataReference = MetadataReference.CreateFromAssemblyInternal(GetType(CompilerServices.NewLateBinding).GetTypeInfo().Assembly)

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return VisualBasicDiagnosticFormatter.Instance
            End Get
        End Property

        Public Overrides ReadOnly Property IdentifierComparer As StringComparer
            Get
                Return CaseInsensitiveComparison.Comparer
            End Get
        End Property

        Public Overrides Function IsCompleteSubmission(tree As SyntaxTree) As Boolean
            Return SyntaxFactory.IsCompleteSubmission(tree)
        End Function

        Public Overrides Function ParseSubmission(text As SourceText, parseOptions As ParseOptions, cancellationToken As CancellationToken) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(text, If(parseOptions, s_defaultOptions), cancellationToken:=cancellationToken)
        End Function

        Private Shared Function GetGlobalImportsForCompilation(script As Script) As IEnumerable(Of GlobalImport)
            ' TODO: remember these per options instance so we don't need to reparse each submission
            ' TODO: get imports out of compilation??? https://github.com/dotnet/roslyn/issues/5854
            Return script.Options.Imports.Select(Function(n) GlobalImport.Parse(n))
        End Function

        Public Overrides Function CreateSubmission(script As Script) As Compilation
            Dim previousSubmission As VisualBasicCompilation = Nothing
            If script.Previous IsNot Nothing Then
                previousSubmission = DirectCast(script.Previous.GetCompilation(), VisualBasicCompilation)
            End If

            Dim diagnostics = DiagnosticBag.GetInstance()
            'Dim references = script.GetReferencesForCompilation(MessageProvider.Instance, diagnostics, s_vbRuntimeReference)
            Dim references = GetReferencesForCompilation(MessageProvider.Instance, diagnostics, script)

            '  TODO report Diagnostics
            diagnostics.Free()

            ' parse:
            Dim tree = SyntaxFactory.ParseSyntaxTree(script.SourceText, If(script.Options.ParseOptions, s_defaultOptions), script.Options.FilePath)

            ' create compilation:
            Dim assemblyName As String = Nothing
            Dim submissionTypeName As String = Nothing
            script.Builder.GenerateSubmissionId(assemblyName, submissionTypeName)

            Dim globalImports = GetGlobalImportsForCompilation(script)

            Dim submission = VisualBasicCompilation.CreateScriptCompilation(
                assemblyName,
                tree,
                references,
                New VisualBasicCompilationOptions(
                    outputKind:=OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName:=Nothing,
                    scriptClassName:=submissionTypeName,
                    globalImports:=globalImports,
                    rootNamespace:="",
                    optionStrict:=OptionStrict.Off,
                    optionInfer:=True,
                    optionExplicit:=True,
                    optionCompareText:=False,
                    embedVbCoreRuntime:=False,
                    optimizationLevel:=script.Options.OptimizationLevel,
                    checkOverflow:=script.Options.CheckOverflow,
                    xmlReferenceResolver:=Nothing, ' don't support XML file references in interactive (permissions & doc comment includes)
                    sourceReferenceResolver:=SourceFileResolver.Default,
                    metadataReferenceResolver:=script.Options.MetadataResolver,
                    assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default).
                    WithIgnoreCorLibraryDuplicatedTypes(True),
                previousSubmission,
                script.ReturnType,
                script.GlobalsType)

            Return submission
        End Function
        ''' <summary>
        ''' Gets the references that need to be assigned to the compilation.
        ''' This can be different than the list of references defined by the <see cref="T:Microsoft.CodeAnalysis.Scripting.ScriptOptions" /> instance.
        ''' </summary>
        Friend Function GetReferencesForCompilation(ByVal messageProvider As CommonMessageProvider, ByVal diagnostics As DiagnosticBag, ByVal script As Script) As ImmutableArray(Of MetadataReference)
            Dim metadataReferences As ImmutableArray(Of MetadataReference)
            Dim metadataResolver As MetadataReferenceResolver = script.Options.MetadataResolver
            Dim instance As ArrayBuilder(Of MetadataReference) = ArrayBuilder(Of MetadataReference).GetInstance()
            Try
                metadataReferences = script.Options.MetadataReferences
                For Each metadataReference In metadataReferences
                    Dim unresolvedMetadataReference As UnresolvedMetadataReference = TryCast(metadataReference, UnresolvedMetadataReference)
                    If (unresolvedMetadataReference Is Nothing) Then
                        instance.Add(metadataReference)
                    Else
                        Dim immutableArray As ImmutableArray(Of PortableExecutableReference) = metadataResolver.ResolveReference(unresolvedMetadataReference.Reference, Nothing, unresolvedMetadataReference.Properties)
                        If (Not immutableArray.IsDefault) Then
                            instance.AddRange(immutableArray)
                        Else
                            diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MetadataFileNotFound, Location.None, New Object() {unresolvedMetadataReference.Reference}))
                        End If
                    End If
                Next
                metadataReferences = instance.ToImmutable()
            Finally
                instance.Free()
            End Try
            Return metadataReferences
        End Function
    End Class

End Namespace
