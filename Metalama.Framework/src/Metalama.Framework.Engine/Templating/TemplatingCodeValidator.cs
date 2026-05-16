// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Templating
{
    public static partial class TemplatingCodeValidator
    {
        public static Task<bool> ValidateAsync(
            ProjectServiceProvider serviceProvider,
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken )
        {
            var compilationContext = serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>().GetInstance( compilation );

            return ValidateAsync( serviceProvider, compilationContext, reportDiagnostic, null, cancellationToken );
        }

        internal static async Task<bool> ValidateAsync(
            ProjectServiceProvider serviceProvider,
            ClassifyingCompilationContext compilationContext,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            CancellationToken cancellationToken )
        {
            // Use the two-phase API but await both phases.
            using var twoPhase = ValidateTwoPhaseAsync( serviceProvider, compilationContext, reportDiagnostic, reportSuppression, cancellationToken );

            var results = await Task.WhenAll( twoPhase.Phase1, twoPhase.Phase2 );

            return results[0] && results[1];
        }

        /// <summary>
        /// Validates the compilation in two phases. Phase 1 validates syntax trees that are recognized by
        /// <see cref="CompileTimeCodeFastDetector"/> as containing compile-time code. Phase 2 validates the
        /// remaining syntax trees. This allows phase 2 to run concurrently with other pipeline operations.
        /// </summary>
        /// <returns>
        /// A <see cref="TwoPhaseValidationResult"/> containing the phase 1 task, phase 2 task, and a method
        /// to cancel phase 2.
        /// </returns>
        internal static TwoPhaseValidationResult ValidateTwoPhaseAsync(
            ProjectServiceProvider serviceProvider,
            ClassifyingCompilationContext compilationContext,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            CancellationToken cancellationToken )
        {
            var taskScheduler = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            var observer = serviceProvider.Global.GetService<ITemplatingCodeValidatorObserver>();
            var semanticModelProvider = compilationContext.SemanticModelProvider;

            // Partition syntax trees into two groups based on CompileTimeCodeFastDetector.
            var phase1Trees = new List<SyntaxTree>();
            var phase2Trees = new List<SyntaxTree>();

            foreach ( var syntaxTree in compilationContext.SourceCompilation.SyntaxTrees )
            {
                var root = syntaxTree.GetRoot( cancellationToken );

                if ( CompileTimeCodeFastDetector.HasCompileTimeCode( root ) )
                {
                    phase1Trees.Add( syntaxTree );
                }
                else
                {
                    phase2Trees.Add( syntaxTree );
                }
            }

            // Create a linked cancellation token source for phase 2 that can be cancelled independently.
            var phase2Cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

            var phase1Task = ValidateSyntaxTreesAsync( phase1Trees, hasCompileTimeCodeFast: true, cancellationToken );
            var phase2Task = ValidateSyntaxTreesAsync( phase2Trees, hasCompileTimeCodeFast: false, phase2Cts.Token );

            return new TwoPhaseValidationResult( phase1Task, phase2Task, phase2Cts );

            async Task<bool> ValidateSyntaxTreesAsync( IEnumerable<SyntaxTree> syntaxTrees, bool hasCompileTimeCodeFast, CancellationToken ct )
            {
                var hasError = false;

                void ValidateSyntaxTree( SyntaxTree syntaxTree )
                {
                    // Skip generated code files.
                    var filePath = syntaxTree.FilePath;

                    var isGeneratedFile =
                        !string.IsNullOrEmpty( filePath )
                        && (filePath.EndsWith( ".g.cs", StringComparison.OrdinalIgnoreCase )
                            || filePath.EndsWith( ".designer.cs", StringComparison.OrdinalIgnoreCase )
                            || filePath.EndsWith( ".generated.cs", StringComparison.OrdinalIgnoreCase )
                            || filePath.IndexOf( "/obj/", StringComparison.OrdinalIgnoreCase ) >= 0
                            || filePath.IndexOf( "\\obj\\", StringComparison.OrdinalIgnoreCase ) >= 0);

                    var isGeneratedBySyntaxTreeOptions =
                        compilationContext.SourceCompilation.Options.SyntaxTreeOptionsProvider?.IsGenerated( syntaxTree, ct )
                        == GeneratedKind.MarkedGenerated;

                    if ( isGeneratedFile || isGeneratedBySyntaxTreeOptions )
                    {
                        observer?.OnSyntaxTreeSkipped();

                        return;
                    }

                    var semanticModel = semanticModelProvider.GetSemanticModel( syntaxTree );

                    if ( !ValidateCore(
                            serviceProvider,
                            semanticModel,
                            compilationContext,
                            reportDiagnostic,
                            reportSuppression,
                            false,
                            false,
                            hasCompileTimeCodeFast,
                            ct ) )
                    {
                        hasError = true;
                    }

                    observer?.OnSyntaxTreeValidated();
                }

                await taskScheduler.RunConcurrentlyAsync( syntaxTrees, ValidateSyntaxTree, ct );

                return !hasError;
            }
        }

        public static void Validate(
            ProjectServiceProvider serviceProvider,
            SemanticModel semanticModel,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            bool reportCompileTimeTreeOutdatedError,
            bool isDesignTime,
            CancellationToken cancellationToken )
        {
            var compilationContext = serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>().GetInstance( semanticModel.Compilation );

            ValidateCoreAndHandleExceptions(
                serviceProvider,
                semanticModel,
                reportDiagnostic,
                reportSuppression,
                reportCompileTimeTreeOutdatedError,
                isDesignTime,
                compilationContext,
                cancellationToken );
        }

        private static void ValidateCoreAndHandleExceptions(
            ProjectServiceProvider serviceProvider,
            SemanticModel semanticModel,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            bool reportCompileTimeTreeOutdatedError,
            bool isDesignTime,
            ClassifyingCompilationContext compilationContext,
            CancellationToken cancellationToken )
        {
            try
            {
                ValidateCore(
                    serviceProvider,
                    semanticModel,
                    compilationContext,
                    reportDiagnostic,
                    reportSuppression,
                    reportCompileTimeTreeOutdatedError,
                    isDesignTime,
                    null,
                    cancellationToken );
            }
            catch ( Exception e )
            {
                var handler = serviceProvider.Global.GetService<ICompileTimeExceptionHandler>();

                if ( handler == null )
                {
                    throw;
                }
                else
                {
                    // It is important to swallow the exception here because this validator is executed on the whole code, even without
                    // aspect, so an exception in this code would have a large impact without any workaround. However, this code has no
                    // other use than reporting diagnostics, so skipping it is safer than failing the compilation. 
                    handler.ReportException( e, reportDiagnostic, true, out _ );
                }
            }
        }

        private static bool ValidateCore(
            ProjectServiceProvider serviceProvider,
            SemanticModel semanticModel,
            ClassifyingCompilationContext compilationContext,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            bool reportCompileTimeTreeOutdatedError,
            bool isDesignTime,
            bool? hasCompileTimeCodeFast,
            CancellationToken cancellationToken )
        {
            if ( !compilationContext.ReferencesMetalamaFramework )
            {
                // There is nothing to validate when the compilation does not reference Metalama. This situation can
                // happen transiently when the IDE analyzes a project whose references are momentarily incomplete,
                // e.g. during a 'git clean' (https://github.com/metalama/Metalama/issues/1630). Proceeding would
                // throw when resolving Metalama symbols.
                return true;
            }

            Visitor visitor = new(
                serviceProvider,
                semanticModel,
                compilationContext,
                reportDiagnostic,
                reportSuppression,
                reportCompileTimeTreeOutdatedError,
                isDesignTime,
                hasCompileTimeCodeFast,
                cancellationToken );

            visitor.Visit( semanticModel.SyntaxTree.GetRoot( cancellationToken ) );

            return !visitor.HasError;
        }
    }
}