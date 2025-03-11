// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed class TemplateCompiler
    {
        private readonly ClassifyingCompilationContext _compilationContext;
        private readonly TemplateProjectManifestBuilder? _templateManifestBuilder;
        private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;
        private readonly ITemplateCompilerObserver? _observer;
        private readonly SerializableTypes _serializableTypes;
        private readonly ILogger _logger;
        private readonly IProjectOptions _options;

        public TemplateCompiler(
            ProjectServiceProvider serviceProvider,
            ClassifyingCompilationContext compilationContext,
            TemplateProjectManifestBuilder? templateManifestBuilder = null )
        {
            this._compilationContext = compilationContext;
            this._templateManifestBuilder = templateManifestBuilder;
            this._syntaxTreeAnnotationMap = new SyntaxTreeAnnotationMap();
            this._logger = serviceProvider.GetLoggerFactory().CompileTime();

            var syntaxSerializationService = serviceProvider.GetRequiredService<SyntaxSerializationService>();
            this._serializableTypes = syntaxSerializationService.GetSerializableTypes( compilationContext.CompilationContext );

            this._observer = serviceProvider.GetService<ITemplateCompilerObserver>();

            this._options = serviceProvider.GetRequiredService<IProjectOptions>();
        }

        public ILocationAnnotationMapBuilder LocationAnnotationMap => this._syntaxTreeAnnotationMap;

        public bool TryAnnotate(
            SyntaxNode sourceSyntaxRoot,
            SemanticModel semanticModel,
            IDiagnosticAdder diagnostics,
            CancellationToken cancellationToken,
            out SyntaxNode annotatedSyntaxRoot,
            out RoslynApiVersion usedApiVersion )
        {
            SyntaxNode currentSyntaxRoot;

            void FixupTreeForDiagnostics()
            {
                // Put the annotated node back into the original tree, so that diagnostics have correct locations.
                var markerAnnotation = new SyntaxAnnotation();

                var annotatedTree = sourceSyntaxRoot.SyntaxTree.GetRoot()
                    .ReplaceNode(
                        sourceSyntaxRoot,
                        currentSyntaxRoot.WithAdditionalAnnotations( markerAnnotation ) );

                currentSyntaxRoot = annotatedTree.GetAnnotatedNodes( markerAnnotation ).Single();
            }

            var maximalAcceptableLanguageVersion = SupportedCSharpVersions.Default;

            if ( !string.IsNullOrWhiteSpace( this._options.TemplateLanguageVersion ) )
            {
                if ( LanguageVersionFacts.TryParse( this._options.TemplateLanguageVersion, out var templateLanguageVersion )
                     && SupportedCSharpVersions.All.Contains( templateLanguageVersion ) )
                {
                    maximalAcceptableLanguageVersion = templateLanguageVersion;
                }
                else
                {
                    diagnostics.Report(
                        GeneralDiagnosticDescriptors.CSharpVersionNotSupported.CreateRoslynDiagnostic(
                            null,
                            (this._options.TemplateLanguageVersion, "MetalamaTemplateLanguageVersion",
                             SupportedCSharpVersions.FormatSupportedVersions()) ) );
                }
            }

            // Verify the language version of the template.
            var versionVerifier = new RoslynVersionSyntaxVerifier( diagnostics, maximalAcceptableLanguageVersion );
            versionVerifier.Visit( sourceSyntaxRoot );
            usedApiVersion = versionVerifier.MaximalUsedVersion;

            // Annotate the syntax tree with symbols.
            if ( !this._syntaxTreeAnnotationMap.TryAnnotateTemplate( sourceSyntaxRoot, semanticModel, diagnostics, cancellationToken, out currentSyntaxRoot ) )
            {
                annotatedSyntaxRoot = currentSyntaxRoot;

                return false;
            }

            FixupTreeForDiagnostics();

            annotatedSyntaxRoot = currentSyntaxRoot;

            // Annotate the syntax tree with info about build- and run-time nodes,
            var annotatorRewriter = new TemplateAnnotator(
                this._compilationContext,
                this._syntaxTreeAnnotationMap,
                diagnostics,
                this._serializableTypes,
                this._templateManifestBuilder,
                cancellationToken );

            annotatedSyntaxRoot = annotatorRewriter.Visit( annotatedSyntaxRoot )!;

            this._observer?.OnAnnotatedSyntaxNode( sourceSyntaxRoot, annotatedSyntaxRoot );

            // Stop if we have any error.
            return annotatorRewriter.Success;
        }

        // Resharper disable once OutParameterValueIsAlwaysDiscarded.Global
        public bool TryCompile(
            string templateName,
            Compilation compileTimeCompilation,
            SyntaxNode sourceSyntaxRoot,
            TemplateCompilerSemantics templateSyntaxKind,
            SemanticModel semanticModel,
            IDiagnosticAdder diagnostics,
            CancellationToken cancellationToken,
            [NotNullWhen( true )] out SyntaxNode? annotatedSyntaxRoot,
            [NotNullWhen( true )] out SyntaxNode? transformedSyntaxRoot,
            out RoslynApiVersion usedApiVersion )
        {
            if ( !this.TryAnnotate( sourceSyntaxRoot, semanticModel, diagnostics, cancellationToken, out annotatedSyntaxRoot, out usedApiVersion ) )
            {
                transformedSyntaxRoot = null;

                return false;
            }

            var sourceDiagnostics = semanticModel.GetDiagnostics( sourceSyntaxRoot.Span, cancellationToken );

            var errors = sourceDiagnostics.Where( d => d is { Severity: DiagnosticSeverity.Error, IsWarningAsError: false } );

            // ReSharper disable PossibleMultipleEnumeration
            if ( errors.Any() )
            {
                // Don't continue with errors in source code (note however that we do the annotation with errors because of real-time syntax highlighting).
                // Diagnostics don't need to be reported because they would be reported by the compiler anyway.
                // Note that it's ok to annotate a template that has errors. This is used by syntax highlighting. 

                annotatedSyntaxRoot = null;
                transformedSyntaxRoot = null;

                // Report the errors to the log.
                if ( this._logger.Trace != null )
                {
                    this._logger.Trace.Log(
                        $"Cannot create a compile-time assembly for '{semanticModel.SyntaxTree.FilePath}' because there are errors in the source code:" );

                    foreach ( var error in errors )
                    {
                        this._logger.Trace.Log( "    " + error );
                    }

                    this._logger.Trace.Log( $"  Compilation id: {DebuggingHelper.GetObjectId( semanticModel.Compilation )}" );

                    this._logger.Trace.Log( "Syntax trees:" );

                    foreach ( var syntaxTree in semanticModel.Compilation.SyntaxTrees )
                    {
                        this._logger.Trace.Log( "   " + syntaxTree.FilePath );
                    }

                    this._logger.Trace.Log( "Compilation references: " );

                    foreach ( var reference in semanticModel.Compilation.References )
                    {
                        switch ( reference )
                        {
                            case PortableExecutableReference:
                                // Skipped because there are too many of them and they are never wrong.
                                break;

                            case CompilationReference compilation:
                                this._logger.Trace.Log( $"Project: {compilation.Display} ({compilation.Compilation.SyntaxTrees.Count()} syntax tree(s))" );

                                break;

                            default:
                                this._logger.Trace.Log( "Other: " + reference );

                                break;
                        }
                    }
                }

                return false;
            }

            // ReSharper restore PossibleMultipleEnumeration

            var compileTimeCompilationContext = compileTimeCompilation.GetCompilationContext();

            // Compile the syntax tree.
            var templateCompilerRewriter = new TemplateCompilerRewriter(
                templateName,
                templateSyntaxKind,
                this._compilationContext,
                this._syntaxTreeAnnotationMap,
                diagnostics,
                compileTimeCompilationContext,
                this._serializableTypes,
                usedApiVersion,
                cancellationToken );

            transformedSyntaxRoot = templateCompilerRewriter.Visit( annotatedSyntaxRoot );

            return transformedSyntaxRoot != null && templateCompilerRewriter.Success;
        }
    }
}