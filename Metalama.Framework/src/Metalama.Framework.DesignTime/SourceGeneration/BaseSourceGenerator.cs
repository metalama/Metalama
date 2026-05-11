// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Compiler;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AnalyzerConfigOptions = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;

namespace Metalama.Framework.DesignTime.SourceGeneration
{
    /// <summary>
    /// Our base implementation of <see cref="ISourceGenerator"/>, which essentially delegates the work to a <see cref="ProjectSourceGenerator"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract partial class BaseSourceGenerator : IIncrementalGenerator
    {
        static BaseSourceGenerator()
        {
            DesignTimeServices.Initialize();
        }

        protected ServiceProvider<IGlobalService> ServiceProvider { get; }

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<ProjectKey, ProjectSourceGenerator?> _projectHandlers = new();
        private readonly TouchIdComparer _touchIdComparer;
        private readonly IProjectOptionsFactory _projectOptionsFactory;
        private readonly DesignTimeExceptionHandler _exceptionHandler;

        protected BaseSourceGenerator( ServiceProvider<IGlobalService> serviceProvider )
        {
            this.ServiceProvider = serviceProvider;
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
            this._touchIdComparer = new TouchIdComparer( this._logger );
            this._projectOptionsFactory = serviceProvider.GetRequiredService<IProjectOptionsFactory>();
            this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
        }

        protected BaseSourceGenerator() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

        private protected abstract ProjectSourceGenerator CreateSourceGeneratorImpl( IProjectOptions projectOptions, ProjectKey projectKey );

        void IIncrementalGenerator.Initialize( IncrementalGeneratorInitializationContext context )
        {
            try
            {
                if ( MetalamaCompilerInfo.IsActive )
                {
                    return;
                }

                this._logger.Trace?.Log( $"Initialize()" );

                var source =
                    context.AnalyzerConfigOptionsProvider.Select(
                            ( x, _ ) =>
                            {
                                var projectOptions = this._projectOptionsFactory.GetProjectOptions( x );
                                this._logger.Trace?.Log( $"Roslyn asks the generated source for '{projectOptions.AssemblyName}'." );

                                return (AnalyzerOptions: x.GlobalOptions, PipelineOptions: projectOptions);
                            } )
                        .Combine( context.CompilationProvider )
                        .Select( ( x, _ ) => (Compilation: x.Right, x.Left.AnalyzerOptions, x.Left.PipelineOptions) )
                        .Select( this.OnGeneratedSourceRequested )
                        .WithComparer( this._touchIdComparer )
                        .Select(
                            ( x, cancellationToken )
                                => x.Options == null
                                    ? SourceGeneratorResult.Empty
                                    : this.GetGeneratedSources( x.Compilation, x.Options, cancellationToken.ToTestable() ) );

                context.RegisterSourceOutput( source, ( productionContext, result ) => result.ProduceContent( productionContext ) );

                this._logger.Trace?.Log( $"Initialize(): completed." );
            }
            catch ( Exception e ) when ( DesignTimeExceptionHandler.MustHandle( e ) )
            {
                this._exceptionHandler.ReportException( e );

                // We rethrow the exception because it is important that the user knows that there was a problem,
                // given that the compilation may be broken.
                throw;
            }
        }

        private (IProjectOptions? Options, Compilation Compilation, string? TouchId) OnGeneratedSourceRequested(
            (Compilation Compilation, AnalyzerConfigOptions AnalyzerOptions, IProjectOptions PipelineOptions) args,
            CancellationToken cancellationToken )
        {
            this._logger.Trace?.Log( $"OnGeneratedSourceRequested('{args.Compilation.AssemblyName}')" );

            if ( !args.AnalyzerOptions.TryGetValue( $"build_property.AssemblyName", out var assemblyNameFromOptions )
                 || string.IsNullOrEmpty( assemblyNameFromOptions ) )
            {
                return (null, args.Compilation, null);
            }

            this.OnGeneratedSourceRequested( args.Compilation, args.PipelineOptions, cancellationToken.ToTestable() );

            var projectKey = args.Compilation.GetProjectKey();

            // The in-process handler is the authoritative source of the latest GUID the pipeline has produced;
            // reading it skips the symbol lookup against the compilation. The compilation lookup is the fallback
            // for cases where the handler hasn't run yet (e.g., first generator invocation after process start).

            if ( !this._projectHandlers.TryGetValue( projectKey, out var handler ) || handler?.LastTouchId is not { } touchId )
            {
                touchId = GetTouchId( args.Compilation );
            }

            this._logger.Trace?.Log( $"OnGeneratedSourceRequested('{args.Compilation.AssemblyName}'): touchId = '{touchId}'" );

            return (args.PipelineOptions, args.Compilation, touchId);
        }

        /// <summary>
        /// This method is called every time the source generator is called. It must decide if the cached result can be served. It must also, if necessary, schedule
        /// a background computation of the compilation.
        /// </summary>
        private protected abstract void OnGeneratedSourceRequested(
            Compilation compilation,
            IProjectOptions options,
            TestableCancellationToken cancellationToken );

        private protected SourceGeneratorResult GetGeneratedSources(
            Compilation compilation,
            IProjectOptions options,
            TestableCancellationToken cancellationToken )
        {
            this._logger.Trace?.Log( $"GetGeneratedSources('{options.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )})." );

            if ( !options.IsFrameworkEnabled )
            {
                // Metalama is not enabled for this project.
                this._logger.Trace?.Log(
                    $"GetGeneratedSources('{options.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}): Metalama not enabled." );

                return SourceGeneratorResult.Empty;
            }

            var projectKey = compilation.GetProjectKey();

            if ( !projectKey.HasHashCode )
            {
                this._logger.Warning?.Log(
                    $"GetGeneratedSources('{options.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}): no syntax tree." );

                return SourceGeneratorResult.Empty;
            }

            // Get or create an IProjectHandler instance.
            if ( !this._projectHandlers.TryGetValue( projectKey, out var projectHandler ) )
            {
                projectHandler = this._projectHandlers.GetOrAdd(
                    projectKey,
                    static ( _, ctx ) =>
                    {
                        if ( ctx.options.IsFrameworkEnabled )
                        {
                            if ( ctx.options.IsDesignTimeEnabled )
                            {
                                return ctx.me.CreateSourceGeneratorImpl( ctx.options, ctx.projectKey );
                            }
                            else
                            {
                                return new OfflineProjectHandler( ctx.me.ServiceProvider, ctx.options, ctx.projectKey );
                            }
                        }
                        else
                        {
                            return null;
                        }
                    },
                    (options, projectKey, me: this) );
            }

            if ( projectHandler == null )
            {
                return SourceGeneratorResult.Empty;
            }

            // Invoke GenerateSources for the project handler.
            var result = projectHandler.GenerateSources( compilation, cancellationToken );

            this._logger.Trace?.Log(
                $"GetGeneratedSources('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}): returned {result}." );

            return result;
        }

        private sealed class TouchIdComparer : IEqualityComparer<(IProjectOptions? Options, Compilation Compilation, string? TouchId)>
        {
            private readonly ILogger _logger;

            public TouchIdComparer( ILogger logger )
            {
                this._logger = logger;
            }

            public bool Equals(
                (IProjectOptions? Options, Compilation Compilation, string? TouchId) x,
                (IProjectOptions? Options, Compilation Compilation, string? TouchId) y )
            {
                var equals = x.TouchId == y.TouchId;

                this._logger.Trace?.Log( $"TouchIdComparer('{x.Options?.AssemblyName}') '{x.TouchId}' {(equals ? "==" : "!=")} '{y.TouchId}'" );

                return equals;
            }

            public int GetHashCode( (IProjectOptions? Options, Compilation Compilation, string? TouchId) obj ) => obj.TouchId?.GetHashCodeOrdinal() ?? 0;
        }

        private static string GetTouchId( Compilation compilation )
        {
            // Look up the marker in the current assembly only so an upstream project's marker (visible through
            // [InternalsVisibleTo]) is ignored.
            var type = compilation.Assembly.GetTypeByMetadataName( TouchFileHelper.MarkerFullTypeName );

            if ( type == null )
            {
                return "";
            }

            var field = type.GetMembers( TouchFileHelper.MarkerFieldName ).OfType<IFieldSymbol>().FirstOrDefault();

            return field?.ConstantValue as string ?? "";
        }
    }
}