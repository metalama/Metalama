// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

public sealed partial class DesignTimeAspectPipeline
{
    internal readonly struct PipelineState
    {
        private readonly DesignTimeAspectPipeline _pipeline;

#pragma warning disable IDE0032 // Don't make auto-property to avoid defensive copies.
        private readonly DependencyGraph _dependencies;

        public DependencyGraph Dependencies => this._dependencies;
#pragma warning restore IDE0032

        private static readonly ImmutableDictionary<string, bool> _emptyCompileTimeSyntaxTrees =
            ImmutableDictionary.Create<string, bool>( StringComparer.Ordinal );

        // Syntax trees that may have compile time code based on namespaces. When a syntax tree is known to be compile-time but
        // has been invalidated, we set its value to false. It allows to know
        // that this specific tree is outdated, which then allows us to display a warning.

        public ImmutableDictionary<string, bool>? CompileTimeSyntaxTrees { get; }

        public FallibleResultWithDiagnostics<AspectPipelineConfiguration>? Configuration { get; }

        internal DesignTimeAspectPipelineStatus Status { get; }

        public ProjectVersion? ProjectVersion { get; }

        public DesignTimeAspectPipelineResult PipelineResult { get; }

        private long SnapshotId { get; }

        internal PipelineState( DesignTimeAspectPipeline pipeline )
        {
            this._pipeline = pipeline;
            this._dependencies = DependencyGraph.Empty;
            this.PipelineResult = new DesignTimeAspectPipelineResult();
            this.CompileTimeSyntaxTrees = null;
            this.Configuration = null;
            this.Status = DesignTimeAspectPipelineStatus.Default;
            this.ProjectVersion = null;
            this.SnapshotId = 0;
        }

        private PipelineState( PipelineState prototype )
        {
            this._pipeline = prototype._pipeline;
            this.ProjectVersion = prototype.ProjectVersion;
            this.CompileTimeSyntaxTrees = prototype.CompileTimeSyntaxTrees;
            this.Configuration = prototype.Configuration;
            this.Status = prototype.Status;
            this.PipelineResult = prototype.PipelineResult;
            this._dependencies = prototype._dependencies;
            this.SnapshotId = prototype.SnapshotId + 1;
        }

        private PipelineState(
            PipelineState prototype,
            FallibleResultWithDiagnostics<AspectPipelineConfiguration> configuration,
            DesignTimeAspectPipelineStatus status ) : this( prototype )
        {
            this.Configuration = configuration;
            this.Status = status;
        }

        private PipelineState(
            PipelineState prototype,
            ImmutableDictionary<string, bool> compileTimeSyntaxTrees,
            DesignTimeAspectPipelineStatus status,
            ProjectVersion projectVersion,
            DesignTimeAspectPipelineResult pipelineResult,
            DependencyGraph dependencies,
            FallibleResultWithDiagnostics<AspectPipelineConfiguration>? configuration )
            : this( prototype )
        {
            this.CompileTimeSyntaxTrees = compileTimeSyntaxTrees;
            this.Status = status;
            this.ProjectVersion = projectVersion;
            this.PipelineResult = pipelineResult;
            this.Configuration = configuration;
            this._dependencies = dependencies;
        }

        private PipelineState( PipelineState prototype, ImmutableDictionary<string, bool> compileTimeSyntaxTrees )
            : this( prototype )
        {
            this.CompileTimeSyntaxTrees = compileTimeSyntaxTrees;
        }

        private PipelineState( PipelineState prototype, DesignTimeAspectPipelineStatus status )
            : this( prototype )
        {
            this.Status = status;
        }

        private PipelineState(
            PipelineState prototype,
            ProjectVersion projectVersion,
            DesignTimeAspectPipelineResult pipelineResult,
            DependencyGraph dependencies ) : this( prototype )
        {
            this.PipelineResult = pipelineResult;
            this.ProjectVersion = projectVersion;
            this._dependencies = dependencies;
        }

        private static IReadOnlyList<SyntaxTree> GetCompileTimeSyntaxTrees(
            ref PipelineState state,
            Compilation compilation,
            TestableCancellationToken cancellationToken )
        {
            List<SyntaxTree> trees = new( state.CompileTimeSyntaxTrees?.Count ?? 8 );

            if ( state.CompileTimeSyntaxTrees == null )
            {
                // The cache has not been set yet, so we need to compute the value from zero.

                if ( state.ProjectVersion?.Compilation != null && state.ProjectVersion.Compilation != compilation )
                {
                    throw new AssertionFailedException( $"Compilation mismatch with '{compilation.Assembly.Identity}'." );
                }

                var newCompileTimeSyntaxTrees = ImmutableDictionary<string, bool>.Empty;

                foreach ( var syntaxTree in compilation.SyntaxTrees )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( CompileTimeCodeFastDetector.HasCompileTimeCode( syntaxTree.GetRoot() ) )
                    {
                        newCompileTimeSyntaxTrees = newCompileTimeSyntaxTrees.Add( syntaxTree.FilePath, true );
                        trees.Add( syntaxTree );
                    }
                }

                state = new PipelineState( state, newCompileTimeSyntaxTrees );
            }
            else
            {
                foreach ( var syntaxTree in compilation.SyntaxTrees )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( state.CompileTimeSyntaxTrees.ContainsKey( syntaxTree.FilePath ) )
                    {
                        trees.Add( syntaxTree );
                    }
                }
            }

            return trees;
        }

        public PipelineState Reset() => new( this._pipeline );

        /// <summary>
        /// Invalidates the cache given a new <see cref="Compilation"/>.
        /// </summary>
        internal async ValueTask<PipelineState> InvalidateCacheForNewCompilationAsync(
            Compilation newCompilation,
            bool invalidateCompilationResult,
            TestableCancellationToken cancellationToken )
        {
            var newStatus = this.Status;
            var newState = this;
            var newConfiguration = this.Configuration;

            // Detect changes in the syntax trees of the tracked compilation.
            var newChanges = await this._pipeline._projectVersionProvider.GetCompilationChangesAsync(
                this.ProjectVersion?.Compilation,
                newCompilation,
                cancellationToken );

            ImmutableDictionary<string, bool> newCompileTimeSyntaxTrees;
            DependencyGraph newDependencyGraph;
            DesignTimeAspectPipelineResult newAspectPipelineResult;

            if ( newChanges.HasCompileTimeCodeChange )
            {
                if ( this.CompileTimeSyntaxTrees == null && newChanges.IsIncremental )
                {
                    throw new AssertionFailedException( "Got an incremental compilation change, but _compileTimeSyntaxTrees is null." );
                }

                // If assembly version changes during loading of VS, we don't want to pause the pipeline.
                if ( newChanges.AssemblyIdentityChanged )
                {
                    OnCompileTimeChange( this._pipeline.Logger, requiresRebuild: false );
                }

                // If there is a compile-time change in references, signal a compile-time change for the current project but do not pause the pipeline.
                if ( !newChanges.ReferencedPortableExecutableChanges.IsEmpty ||
                     newChanges.ReferencedCompilationChanges.Any( c => c.Value.HasCompileTimeCodeChange ) )
                {
                    OnCompileTimeChange( this._pipeline.Logger, false );
                }

                var compileTimeSyntaxTreesBuilder = this.CompileTimeSyntaxTrees?.ToBuilder()
                                                    ?? ImmutableDictionary.CreateBuilder<string, bool>( StringComparer.Ordinal );

                foreach ( var changeEntry in newChanges.SyntaxTreeChanges )
                {
                    var change = changeEntry.Value;

                    switch ( change.CompileTimeChangeKind )
                    {
                        case CompileTimeChangeKind.None:
                            if ( change.HasCompileTimeCode )
                            {
                                // When a syntax tree is known to be compile-time but has been invalidated, we don't remove it from the dictionary,
                                // but we set its value to null. It allows to know that this specific tree is outdated,
                                // which then allows us to display a warning.
                                compileTimeSyntaxTreesBuilder[change.FilePath] = false;

                                // We require an external build because we don't want to invalidate the pipeline configuration at
                                // each keystroke.
                                this._pipeline.Logger.Trace?.Log(
                                    $"Compile-time change detected: {change.FilePath} has changed. Old hash: {change.OldHash}, new hash: {change.NewHash}." );

                                // Generated files may change during the startup sequence, and it is safe to reset the pipeline in this case.
                                var requiresRebuild = !change.FilePath.EndsWith( ".g.cs", StringComparison.OrdinalIgnoreCase );

                                OnCompileTimeChange( this._pipeline.Logger, requiresRebuild );
                            }

                            break;

                        case CompileTimeChangeKind.NewlyCompileTime:
                            // We don't require an external rebuild when a new syntax tree is added because Roslyn does not give us a complete
                            // compilation in the first call in the Visual Studio initializations sequence. Roslyn calls us later with
                            // a complete compilation, but we don't want to bother the user with the need of an external build.
                            compileTimeSyntaxTreesBuilder[change.FilePath] = true;
                            this._pipeline.Logger.Trace?.Log( $"Compile-time change detected: {change.FilePath} is a new compile-time syntax tree." );
                            OnCompileTimeChange( this._pipeline.Logger, false );

                            break;

                        case CompileTimeChangeKind.NoLongerCompileTime:
                            compileTimeSyntaxTreesBuilder.Remove( change.FilePath );
                            this._pipeline.Logger.Trace?.Log( $"Compile-time change detected: : {change.FilePath} no longer contains compile-time code." );
                            OnCompileTimeChange( this._pipeline.Logger, false );

                            break;
                    }
                }

                newCompileTimeSyntaxTrees = compileTimeSyntaxTreesBuilder.ToImmutable();

                if ( invalidateCompilationResult )
                {
                    newDependencyGraph = DependencyGraph.Empty;
                    newAspectPipelineResult = new DesignTimeAspectPipelineResult();
                }
                else
                {
                    // The pipeline is paused. We do not invalidate the results to we can still serve the old ones.

                    newDependencyGraph = this._dependencies;
                    newAspectPipelineResult = this.PipelineResult;
                }
            }
            else
            {
                // Compile-time trees are unchanged.
                newCompileTimeSyntaxTrees = this.CompileTimeSyntaxTrees ?? _emptyCompileTimeSyntaxTrees;

                if ( newChanges.HasChange )
                {
                    var invalidator = this.PipelineResult.ToInvalidator();

                    newDependencyGraph = await this._pipeline._projectVersionProvider.ProcessCompilationChangesAsync(
                        newChanges,
                        this._dependencies,
                        invalidator.InvalidateSyntaxTree,
                        false,
                        cancellationToken );

                    newAspectPipelineResult = invalidator.ToImmutable();
                }
                else
                {
                    newDependencyGraph = this._dependencies;
                    newAspectPipelineResult = this.PipelineResult;
                }
            }

            // Return the new state.

            newState = new PipelineState(
                newState,
                newCompileTimeSyntaxTrees,
                newStatus,
                newChanges.NewProjectVersion,
                newAspectPipelineResult,
                newDependencyGraph,
                newConfiguration );

            return newState;

            // Local method called when a change is detected in compile-time code.
            void OnCompileTimeChange( ILogger logger, bool requiresRebuild )
            {
                invalidateCompilationResult = false;

                if ( newState.Status is DesignTimeAspectPipelineStatus.Ready or DesignTimeAspectPipelineStatus.Default )
                {
                    logger.Trace?.Log( $"DesignTimeAspectPipeline.InvalidateCache('{newCompilation.AssemblyName}'): compile-time change detected." );

                    var pipeline = newState._pipeline;

                    if ( requiresRebuild )
                    {
                        logger.Trace?.Log( "Pausing the pipeline." );

                        newStatus = DesignTimeAspectPipelineStatus.Paused;

                        if ( pipeline.ProjectOptions.BuildTouchFile != null && File.Exists( pipeline.ProjectOptions.BuildTouchFile ) )
                        {
                            if ( File.Exists( pipeline.ProjectOptions.BuildTouchFile ) )
                            {
                                using ( MutexHelper.WithGlobalLock( pipeline.ProjectOptions.BuildTouchFile ) )
                                {
                                    File.Delete( pipeline.ProjectOptions.BuildTouchFile );
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.Trace?.Log( "Requiring an in-process configuration refresh." );

                        newConfiguration = null;
                        newStatus = DesignTimeAspectPipelineStatus.Default;
                    }
                }
            }
        }

        internal static FallibleResultWithDiagnostics<AspectPipelineConfiguration> GetConfiguration(
            ref PipelineState state,
            Compilation compilation,
            bool ignoreStatus,
            TestableCancellationToken cancellationToken )
        {
            if ( state.Status == DesignTimeAspectPipelineStatus.Paused && ignoreStatus )
            {
                state = new PipelineState( state._pipeline );
            }

            state._pipeline.Logger.Trace?.Log(
                $"DesignTimeAspectPipeline.TryGetConfiguration( '{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )})" );

            if ( state.Configuration == null )
            {
                // If we don't have any configuration, we will build one, because this is the first time we are called.

                var compileTimeTrees = GetCompileTimeSyntaxTrees( ref state, compilation, cancellationToken );

                state._pipeline._observer?.OnInitializePipeline( compilation );

                var diagnosticAdder = new DiagnosticBag();

                var initializeSuccessful = state._pipeline.TryInitialize(
                    diagnosticAdder,
                    compilation,
                    compileTimeTrees,
                    cancellationToken,
                    out var configuration );

                var errors = diagnosticAdder.Where( d => d is { Severity: DiagnosticSeverity.Error, IsSuppressed: false } ).ToReadOnlyList();

                foreach ( var error in errors )
                {
                    state._pipeline.Logger.Warning?.Log( $"DesignTimeAspectPipeline.TryGetConfiguration error: {error}" );
                }

                // Publish compilation errors. This may create some chaos at the receiving end because compilations are unordered.
                state._pipeline._eventHub?.PublishCompileTimeErrors(
                    state._pipeline.ProjectKey,
                    errors
                        .SelectAsImmutableArray( d => new DiagnosticData( d ) ) );

                if ( !initializeSuccessful )
                {
                    // A failure here means an error or a cache miss.

                    state._pipeline.Logger.Warning?.Log(
                        $"DesignTimeAspectPipeline.TryGetConfiguration('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}) failed: cannot initialize." );

                    return FallibleResultWithDiagnostics<AspectPipelineConfiguration>.Failed( diagnosticAdder.ToImmutableArray() );
                }
                else
                {
                    state._pipeline.Logger.Trace?.Log(
                        $"DesignTimeAspectPipeline.TryGetConfiguration('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}) succeeded with new configuration {DebuggingHelper.GetObjectId( configuration )}: "
                        +
                        $"the compilation contained {compilation.SyntaxTrees.Count()} syntax trees: {string.Join( ", ", compilation.SyntaxTrees.Select( t => Path.GetFileName( t.FilePath ) ) )}" );

                    var result = FallibleResultWithDiagnostics<AspectPipelineConfiguration>.Succeeded( configuration!, diagnosticAdder.ToImmutableArray() );
                    state = new PipelineState( state, result, DesignTimeAspectPipelineStatus.Ready );

                    return result;
                }
            }
            else if ( !state.Configuration.Value.IsSuccessful )
            {
                // We have a cached configuration, but a failed one.

                state._pipeline.Logger.Warning?.Log(
                    $"DesignTimeAspectPipeline.TryGetConfiguration('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}) failed: a previous initialization of the pipeline has failed and there is no change." );

                return FallibleResultWithDiagnostics<AspectPipelineConfiguration>.Failed( state.Configuration.Value.Diagnostics );
            }
            else
            {
                if ( state.Status == DesignTimeAspectPipelineStatus.Paused )
                {
                    // We have an outdated configuration because the pipeline is paused.

                    state._pipeline.Logger.Warning?.Log(
                        $"DesignTimeAspectPipeline.TryGetConfiguration('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}) failed: the pipeline is paused." );

                    return FallibleResultWithDiagnostics<AspectPipelineConfiguration>.Failed( ImmutableArray<Diagnostic>.Empty );
                }
                else
                {
                    // We have a valid configuration and it is not outdated.

                    state._pipeline.Logger.Trace?.Log(
                        $"DesignTimeAspectPipeline.TryGetConfiguration('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}) returned existing configuration {DebuggingHelper.GetObjectId( state.Configuration.Value.Value )}." );

                    return state.Configuration.Value;
                }
            }
        }

        /// <summary>
        /// Executes the pipeline.
        /// </summary>
        public static async Task<( FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState> CompilationResult, PipelineState NewState)>
            ExecuteAsync(
                PipelineState state,
                PartialCompilation compilation,
                DesignTimeProjectVersion projectVersion,
                TestableCancellationToken cancellationToken )
        {
            if ( state.Status == DesignTimeAspectPipelineStatus.Paused )
            {
                throw new InvalidOperationException();
            }

            var getConfigurationResult = GetConfiguration( ref state, compilation.Compilation, false, cancellationToken );

            if ( !getConfigurationResult.IsSuccessful )
            {
                if ( state._pipeline.Logger.Trace != null )
                {
                    var errors = getConfigurationResult.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToReadOnlyList();

                    state._pipeline.Logger.Trace?.Log( $"GetConfiguration('{compilation.Compilation.AssemblyName}') failed: {errors.Count} errors reported." );

                    foreach ( var diagnostic in errors )
                    {
                        state._pipeline.Logger.Trace?.Log( diagnostic.ToString() );
                    }
                }

                state = new PipelineState( state, getConfigurationResult, DesignTimeAspectPipelineStatus.Default );

                return (FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed( getConfigurationResult.Diagnostics ), state);
            }

            DiagnosticBag diagnosticBag = new();

            // Report diagnostics from the configuration even if the configuration is successful.
            diagnosticBag.Report( getConfigurationResult.Diagnostics );

            var configuration = getConfigurationResult.Value;

            // Execute the pipeline.
            var dependencyCollector = new DependencyCollector(
                configuration.ServiceProvider.WithService( projectVersion ),
                projectVersion.ProjectVersion,
                compilation );

            compilation.DerivedTypes.PopulateDependencies( dependencyCollector );
            var serviceProvider = configuration.ServiceProvider.WithServices( dependencyCollector, projectVersion );

            var pipelineResult = await state._pipeline.ExecuteAsync(
                compilation,
                diagnosticBag,
                configuration.WithServiceProvider( serviceProvider ),
                cancellationToken );

            var pipelineResultValue = pipelineResult.IsSuccessful ? pipelineResult.Value : null;

#if DEBUG
            dependencyCollector.Freeze();
#endif

            var additionalSyntaxTrees = pipelineResultValue switch
            {
                null => ImmutableArray<IntroducedSyntaxTree>.Empty,
                _ => pipelineResultValue.AdditionalSyntaxTrees
            };

            var inheritableAspectInstances =
                pipelineResultValue?.ExternallyInheritableAspects.SelectAsImmutableArray( i => new InheritableAspectInstance( i ) )
                ?? ImmutableArray<InheritableAspectInstance>.Empty;

            var inheritableOptions =
                pipelineResultValue?.LastCompilationModel.HierarchicalOptionsManager.AssertNotNull()
                    .GetInheritableOptions( pipelineResultValue.LastCompilationModel, true )
                    .ToReadOnlyList() ?? ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty;

            var extensions = pipelineResultValue?.TransitiveContributors ?? ImmutableArray<ITransitivePipelineContributor>.Empty;

            var immutableUserDiagnostics = new ImmutableUserDiagnosticList(
                diagnosticBag.ToImmutableArray(),
                pipelineResultValue?.Diagnostics.DiagnosticSuppressions,
                pipelineResultValue?.Diagnostics.Extensions );

            var aspectInstances = pipelineResultValue?.AspectInstances.ToImmutableArray() ?? ImmutableArray<IAspectInstance>.Empty;

            var transformations = pipelineResultValue?.Transformations ?? ImmutableArray<ITransformationBase>.Empty;

            var annotations = pipelineResultValue?.Annotations
                              ?? ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty.WithKeyComparer(
                                  RefEqualityComparer<IDeclaration>.Default );

            Invariant.Assert( annotations.KeyComparer is IRefEqualityComparer );

            var result = new DesignTimePipelineExecutionResult(
                compilation.SyntaxTrees,
                additionalSyntaxTrees,
                immutableUserDiagnostics,
                inheritableAspectInstances,
                inheritableOptions,
                extensions,
                aspectInstances,
                transformations,
                annotations );

            // Update the dependency graph with results of the pipeline.
            DependencyGraph newDependencies;

            if ( pipelineResult.IsSuccessful )
            {
                if ( state._dependencies.IsUninitialized )
                {
                    newDependencies = DependencyGraph.Create( dependencyCollector );
                }
                else
                {
                    newDependencies = state._dependencies.Update( dependencyCollector );
                }
            }
            else
            {
                newDependencies = state._dependencies;
            }

            // We intentionally commit the pipeline state here so that the caller, not us, can decide what part of the work should be committed
            // in case of cancellation. From our point of view, this is a safe place to commit.
            state = state.SetPipelineResult( compilation, result, newDependencies, projectVersion, getConfigurationResult.Value );

            return (new DesignTimeAspectPipelineResultAndState(
                        state.ProjectVersion.AssertNotNull(),
                        state.PipelineResult,
                        state.Status,
                        configuration ), state);
        }

        private PipelineState SetPipelineResult(
            PartialCompilation compilation,
            DesignTimePipelineExecutionResult pipelineResult,
            DependencyGraph dependencies,
            DesignTimeProjectVersion projectVersion,
            AspectPipelineConfiguration configuration )
        {
            var compilationResult = this.PipelineResult.Update( compilation, projectVersion, pipelineResult, configuration );

            return new PipelineState( this, (ProjectVersion) projectVersion.ProjectVersion, compilationResult, dependencies );
        }

        public PipelineState Pause()
        {
            return new PipelineState( this, DesignTimeAspectPipelineStatus.Paused );
        }
    }
}