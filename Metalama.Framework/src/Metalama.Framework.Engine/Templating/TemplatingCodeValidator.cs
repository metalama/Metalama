// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Templating
{
    public static partial class TemplatingCodeValidator
    {
        internal static async Task<bool> ValidateAsync(
            ProjectServiceProvider serviceProvider,
            ClassifyingCompilationContext compilationContext,
            Action<Diagnostic> reportDiagnostic,
            Action<ScopedSuppression>? reportSuppression,
            CancellationToken cancellationToken )
        {
            var taskScheduler = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            var semanticModelProvider = compilationContext.SemanticModelProvider;

            var hasError = false;

            void ValidateSyntaxTree( SyntaxTree syntaxTree )
            {
                var semanticModel = semanticModelProvider.GetSemanticModel( syntaxTree );

                if ( !ValidateCore(
                        serviceProvider,
                        semanticModel,
                        compilationContext,
                        reportDiagnostic,
                        reportSuppression,
                        false,
                        false,
                        cancellationToken ) )
                {
                    hasError = true;
                }
            }

            await taskScheduler.RunConcurrentlyAsync( compilationContext.SourceCompilation.SyntaxTrees, ValidateSyntaxTree, cancellationToken );

            return !hasError;
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
            CancellationToken cancellationToken )
        {
            Visitor visitor = new(
                serviceProvider,
                semanticModel,
                compilationContext,
                reportDiagnostic,
                reportSuppression,
                reportCompileTimeTreeOutdatedError,
                isDesignTime,
                cancellationToken );

            visitor.Visit( semanticModel.SyntaxTree.GetRoot( cancellationToken ) );

            return !visitor.HasError;
        }
    }
}