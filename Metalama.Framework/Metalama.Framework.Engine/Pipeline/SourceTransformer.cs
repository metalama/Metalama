// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Compiler;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DiagnosticFilter = Metalama.Compiler.DiagnosticFilter;
using IExceptionReporter = Metalama.Backstage.Telemetry.IExceptionReporter;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// The main compile-time entry point of Metalama. An implementation of Metalama.Compiler's <see cref="ISourceTransformer"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[UsedImplicitly]
public sealed partial class SourceTransformer : ISourceTransformerWithServices
{
    public IServiceProvider InitializeServices( InitializeServicesContext context )
    {
        if ( !BackstageServiceFactoryInitializer.IsInitialized )
        {
            var dotNetSdkDirectory = GetDotNetSdkDirectory( context.AnalyzerConfigOptionsProvider );

            var applicationInfo = new SourceTransformerApplicationInfo( context.Options.IsLongRunningProcess );

            var backstageOptions = new BackstageInitializationOptions( applicationInfo )
            {
                AddLicensing = false, AddUserInterface = true, AddSupportServices = true, DotNetSdkDirectory = dotNetSdkDirectory
            };

            BackstageServiceFactoryInitializer.Initialize( backstageOptions );
        }

        var backstageServiceProvider = BackstageServiceFactory.ServiceProvider;

        return new CompilerServiceProvider( backstageServiceProvider, context.AnalyzerConfigOptionsProvider );
    }

    private static string? GetDotNetSdkDirectory( AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider )
    {
        if ( !analyzerConfigOptionsProvider.GlobalOptions.TryGetValue( "build_property.NETCoreSdkBundledVersionsProps", out var propsFilePath )
             || string.IsNullOrEmpty( propsFilePath ) )
        {
            return null;
        }

        return Path.GetFullPath( Path.GetDirectoryName( propsFilePath )! );
    }

    private static void ReportException( Exception e, IServiceProvider serviceProvider, bool throwReporterExceptions )
    {
        try
        {
            var reporter = serviceProvider.GetBackstageService<IExceptionReporter>();
            reporter?.ReportException( e );
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
            var projectOptions = context.ProjectOptions;

            var projectServiceProvider = globalServices
                .WithProjectScopedServices( projectOptions, context.Compilation );

            using CompileTimeAspectPipeline pipeline = new( projectServiceProvider );

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
            var isHandled = false;

            globalServices
                .GetService<ICompileTimeExceptionHandler>()
                ?.ReportException( e, context.ReportDiagnostic, false, out isHandled );

            if ( !isHandled )
            {
                throw;
            }
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
                File.Delete( deletedAuxiliaryFile );
            }

            foreach ( var file in pipelineResult.AdditionalCompilationOutputFiles )
            {
                var fullPath = GetFileFullPath( file );
                Directory.CreateDirectory( Path.GetDirectoryName( fullPath )! );
                using var stream = File.Open( fullPath, FileMode.Create );
                file.WriteToStream( stream );
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
                        ( in DiagnosticFilteringRequest request ) =>
                            suppression.Matches(
                                request.Diagnostic,
                                request.Compilation,
                                filter => userCodeInvoker.Invoke( filter, executionContext! ),
                                declarationId ) ) );
            }
        }
    }
}