// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Compiler;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DiagnosticFilter = Metalama.Compiler.DiagnosticFilter;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// The main compile-time entry point of Metalama. An implementation of Metalama.Compiler's <see cref="ISourceTransformer"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[UsedImplicitly]
public sealed partial class SourceTransformer : ISourceTransformerWithServices
{
    private static readonly object _initializeSync = new();

    public IServiceProvider InitializeServices( InitializeServicesContext context )
    {
        lock ( _initializeSync )
        {
            if ( !BackstageServiceFactoryInitializer.IsInitialized )
            {
                var applicationInfo = new SourceTransformerApplicationInfo( context.Options.IsLongRunningProcess );

                var backstageOptions = new BackstageInitializationOptions( applicationInfo )
                {
                    AddLicensing = false, 
                    AddUserInterface = true, 
                    AddSupportServices = true,

                    // The command-line compiler opens the welcome page the first time telemetry activates (#1701).
                    OpenWelcomePage = true
                };

                BackstageServiceFactoryInitializer.Initialize( backstageOptions );
            }
        }

        var backstageServiceProvider = BackstageServiceFactory.ServiceProvider;

        return new CompilerServiceProvider( backstageServiceProvider, context.AnalyzerConfigOptionsProvider );
    }

    private static void ReportException( Exception e, IServiceProvider serviceProvider, bool throwReporterExceptions )
    {
        try
        {
            // This is the compiler outer fallback (the per-directory context could not be set up): report through the
            // tooling policy. See #1701.
            serviceProvider.ReportToolingException( e );
        }
        catch ( Exception reporterException )
        {
            if ( throwReporterExceptions )
            {
                throw new AggregateException( e, reporterException );
            }
        }
    }

    public void Execute( TransformerContext context ) => Execute( new TransformerContextAdapter( context ) );

    internal static void Execute( ITransformerContext context )
    {
        var globalServices = context.ServiceProvider.Underlying;

        try
        {
            // Test-only fault injection point exercising the global (outer) handling layer. No-op in production. See #1701.
            globalServices.GetService<ITestFaultInjector>()?.InjectFault( FaultInjectionPoints.SourceTransformerEntry );

            var projectOptions = context.ProjectOptions;

            // The compile-time exception handler is a project-scoped service, so it can resolve the project options and
            // the per-project telemetry context (honoring the repository metalama.json opt-out). See #1701.
            var projectServiceProvider = globalServices
                .WithProjectScopedServices( projectOptions, context.Compilation )
                .WithService( sp => new CompileTimeExceptionHandler( sp ) );

            try
            {
                // Test-only fault injection point exercising the project-scoped (inner) handling layer. No-op in
                // production. See #1701.
                globalServices.GetService<ITestFaultInjector>()?.InjectFault( FaultInjectionPoints.CompileTimePipeline );

                using var pipeline = CompileTimeAspectPipeline.Create( projectServiceProvider );

                var taskRunner = globalServices.GetRequiredService<ITaskRunner>();

                var suppressions = new ConcurrentQueue<ScopedSuppression>();

                // ReSharper disable once AccessToDisposedClosure

                var pipelineResult =
                    taskRunner.RunSynchronously(
                        () => pipeline.ExecuteAsync(
                            context.ReportDiagnostic,
                            suppressions.Enqueue,
                            context.Compilation,
                            context.Resources,
                            TestableCancellationToken.None ) );

                HandleSuppressions( context, projectServiceProvider, suppressions );

                if ( pipelineResult.IsSuccessful )
                {
                    context.AddResources( pipelineResult.Value.AdditionalResources );
                    context.AddSyntaxTreeTransformations( pipelineResult.Value.SyntaxTreeTransformations );
                    HandleAdditionalCompilationOutputFiles( projectOptions, pipelineResult.Value );
                    HandleSuppressions( context, projectServiceProvider, pipelineResult.Value.DiagnosticSuppressions );
                }
            }
            catch ( Exception e )
            {
                // The project scope is known: report through the project-scoped handler, which writes the rich diagnostic
                // and captures telemetry through the per-project context.
                var isHandled = false;

                projectServiceProvider
                    .GetService<ICompileTimeExceptionHandler>()
                    ?.ReportException( e, context.ReportDiagnostic, false, out isHandled );

                if ( !isHandled )
                {
                    throw;
                }
            }
        }
        catch ( Exception e )
        {
            // The project scope could not be established (e.g. building the project service provider failed): there is no
            // project context. Report through a handler built from the global services — it writes the local report, emits
            // the diagnostic and reports tooling telemetry — then swallow the exception so the compiler does not crash.
            new CompileTimeExceptionHandler( globalServices ).ReportException( e, context.ReportDiagnostic, false, out _ );
        }
        finally
        {
            globalServices.GetLoggerFactory().Flush();
        }
    }

    private static void HandleAdditionalCompilationOutputFiles( IProjectOptions projectOptions, CompileTimeAspectPipelineResult? pipelineResult )
    {
        if ( pipelineResult == null || projectOptions.AdditionalCompilationOutputDirectory == null )
        {
            return;
        }

        try
        {
            var existingFiles = new HashSet<string>();

            if ( Directory.Exists( projectOptions.AdditionalCompilationOutputDirectory ) )
            {
                foreach ( var existingAuxiliaryFile in Directory.GetFiles(
                             projectOptions.AdditionalCompilationOutputDirectory,
                             "*",
                             SearchOption.AllDirectories ) )
                {
                    existingFiles.Add( existingAuxiliaryFile );
                }
            }

            var finalFiles = new HashSet<string>();

            foreach ( var file in pipelineResult.AdditionalCompilationOutputFiles )
            {
                var fullPath = GetFileFullPath( file );
                finalFiles.Add( fullPath );
            }

            foreach ( var deletedAuxiliaryFile in existingFiles.Except( finalFiles ) )
            {
                try
                {
                    // Remove read-only attribute before deleting.
                    File.SetAttributes( deletedAuxiliaryFile, FileAttributes.Normal );
                    File.Delete( deletedAuxiliaryFile );
                }
                catch ( FileNotFoundException )
                {
                    // The file may have been deleted between Directory.GetFiles and here.
                }
            }

            foreach ( var file in pipelineResult.AdditionalCompilationOutputFiles )
            {
                var fullPath = GetFileFullPath( file );
                Directory.CreateDirectory( Path.GetDirectoryName( fullPath )! );

                if ( File.Exists( fullPath ) )
                {
                    // Remove read-only attribute before overwriting.
                    File.SetAttributes( fullPath, FileAttributes.Normal );
                }

                using ( var stream = File.Open( fullPath, FileMode.Create ) )
                {
                    file.WriteToStream( stream );
                }

                // Set the file as read-only so we have no temptation to edit it.
                File.SetAttributes( fullPath, FileAttributes.ReadOnly );
            }

            string GetFileFullPath( AdditionalCompilationOutputFile file )
            {
                return Path.Combine( projectOptions.AdditionalCompilationOutputDirectory, file.Kind.ToString(), file.Path );
            }
        }
        catch
        {
            // TODO: Warn.
        }
    }

    private static void HandleSuppressions(
        ITransformerContext context,
        ProjectServiceProvider projectServiceProvider,
        IEnumerable<ScopedSuppression> diagnosticSuppressions )
    {
        var userCodeInvoker = projectServiceProvider.GetRequiredService<UserCodeInvoker>();

        var compilationContext = context.Compilation.GetCompilationContext();

        foreach ( var suppression in diagnosticSuppressions )
        {
            var declarationId = suppression.ScopeSymbol.GetSerializableId();

            UserCodeExecutionContext? executionContext = null;

            if ( suppression.Suppression.Filter != null )
            {
                executionContext = new UserCodeExecutionContext(
                    projectServiceProvider,
                    UserCodeDescription.Create( "evaluating suppression filter for {0} on {1}", suppression.Suppression.Definition, suppression.ScopeSymbol ),
                    compilationContext );
            }

            var suppressionDescriptor = SuppressionFactories.CreateDescriptor( suppression.Suppression.Definition.SuppressedDiagnosticId );

            foreach ( var syntaxReference in suppression.ScopeSymbol.DeclaringSyntaxReferences )
            {
                context.RegisterDiagnosticFilter(
                    new DiagnosticFilter(
                        suppressionDescriptor,
                        syntaxReference.SyntaxTree.FilePath,
                        ( in request ) =>
                            suppression.Matches(
                                request.Diagnostic,
                                request.Compilation,
                                filter => userCodeInvoker.Invoke( filter, executionContext! ),
                                declarationId ) ) );
            }
        }
    }
}