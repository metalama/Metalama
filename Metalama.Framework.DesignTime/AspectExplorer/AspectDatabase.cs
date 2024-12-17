// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Introspection;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.AspectExplorer;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable: WeakCache does not need to be disposed
public sealed class AspectDatabase : IGlobalService, IRpcApi
#pragma warning restore CA1001
{
    private readonly ILogger _logger;
    private readonly DesignTimeAspectPipelineFactory _pipelineFactory;
    private readonly WorkspaceProvider _workspaceProvider;
    private readonly AnalysisProcessEventHub _eventHub;
    private readonly DesignTimeExceptionHandler _exceptionHandler;

    private readonly WeakCache<Compilation, ImmutableArray<IIntrospectionAspectInstance>> _aspectInstanceCache = new();

    internal AspectDatabase( GlobalServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "AspectDatabase" );
        this._pipelineFactory = serviceProvider.GetRequiredService<DesignTimeAspectPipelineFactory>();
        this._workspaceProvider = serviceProvider.GetRequiredService<WorkspaceProvider>();
        this._eventHub = serviceProvider.GetRequiredService<AnalysisProcessEventHub>();
        this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
    }

    private async Task<(DesignTimeAspectPipeline Pipeline, Compilation Compilation)?> GetPipelineAndCompilationAsync(
        ProjectKey projectKey,
        CancellationToken cancellationToken )
    {
        var project = await this._workspaceProvider.GetProjectAsync( projectKey, cancellationToken );

        if ( project == null )
        {
            this._logger.Warning?.Log( $"GetPipelineAndCompilationAsync('{projectKey}'): cannot get the project '{projectKey}'." );

            return null;
        }

        var pipeline = this._pipelineFactory.GetOrCreatePipeline( project, cancellationToken.ToTestable() );

        if ( pipeline == null )
        {
            this._logger.Warning?.Log( $"GetPipelineAndCompilationAsync('{projectKey}'): cannot get the pipeline for project '{projectKey}'." );

            return null;
        }

        var compilation = await project.GetCompilationAsync( cancellationToken );

        if ( compilation == null )
        {
            this._logger.Warning?.Log( $"GetPipelineAndCompilationAsync('{projectKey}'): cannot get the compilation for project '{projectKey}'." );

            return null;
        }

        return (pipeline, compilation);
    }

    public async Task<IEnumerable<string>> GetAspectClassesAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        if ( await this.GetPipelineAndCompilationAsync( projectKey, cancellationToken ) is not var (pipeline, compilation) )
        {
            return [];
        }

        await pipeline.GetConfigurationAsync(
            PartialCompilation.CreateComplete( compilation ),
            false,
            AsyncExecutionContext.Get(),
            cancellationToken.ToTestable() );

        var aspectClasses = pipeline.AspectClasses ?? [];

        var aspectClassesIds = aspectClasses
            .OfType<AspectClass>()
            .Where( aspectClass => !aspectClass.IsAbstract )
            .Select( aspectClass => aspectClass.TypeId.Id );

        var fabrics = pipeline.Fabrics ?? ImmutableArray<string>.Empty;
        var fabricsIds = fabrics.Select( fabric => compilation.GetTypeByMetadataName( fabric )?.GetSerializableTypeId().Id ).WhereNotNull();

        return aspectClassesIds.Concat( fabricsIds ).ToArray();
    }

    public async Task<IEnumerable<AspectDatabaseAspectInstance>> GetAspectInstancesAsync(
        ProjectKey projectKey,
        string aspectClassAssembly,
        SerializableTypeId aspectClass,
        CancellationToken cancellationToken )
    {
        if ( await this.GetPipelineAndCompilationAsync( projectKey, cancellationToken ) is not var (designTimePipeline, compilation) )
        {
            return [];
        }

        if ( !this._aspectInstanceCache.TryGetValue( compilation, out var aspectInstances ) )
        {
            var designTimeConfiguration = await designTimePipeline.GetConfigurationAsync(
                PartialCompilation.CreateComplete( compilation ),
                false,
                AsyncExecutionContext.Get(),
                cancellationToken.ToTestable() );

            if ( !designTimeConfiguration.IsSuccessful )
            {
                this._aspectInstanceCache.TryAdd( compilation, ImmutableArray<IIntrospectionAspectInstance>.Empty );

                return [];
            }

            var pipeline = new IntrospectionAspectPipeline( designTimeConfiguration.Value.ServiceProvider, this._pipelineFactory.Domain, null );

            try
            {
                var result = await pipeline.ExecuteAsync(
                    PartialCompilation.CreateComplete( compilation ),
                    designTimeConfiguration.Value,
                    cancellationToken.ToTestable() );

                // We need to call Distinct() here, because aspects with multiple layers show up multiple times.
                aspectInstances = result.AspectInstances.Where( i => !i.IsSkipped ).Distinct().ToImmutableArray();
            }
            catch ( Exception ex )
            {
                this._exceptionHandler.ReportException( ex );

                aspectInstances = ImmutableArray<IIntrospectionAspectInstance>.Empty;
            }

            this._aspectInstanceCache.TryAdd( compilation, aspectInstances );
        }

        var compilationContext = compilation.GetCompilationContext();
        var typeIdResolver = compilationContext.SerializableTypeIdResolver;

        if ( !typeIdResolver.TryResolveId( aspectClass, out var aspectTypeSymbol ) )
        {
            this._logger.Warning?.Log( $"Could not resolve '{aspectClass}'." );

            return [];
        }

        if ( aspectTypeSymbol.ContainingAssembly.Name != aspectClassAssembly )
        {
            this._logger.Trace?.Log( $"Assembly mismatch: '{aspectTypeSymbol.ContainingAssembly.Name}' != '{aspectClassAssembly}'." );

            return [];
        }

        var aspectClassFullName = aspectTypeSymbol.GetReflectionFullName();

        var transformationAspectInstances = aspectInstances
            .Where( aspectInstance => aspectInstance.AspectClass.FullName == aspectClassFullName )
            .Select(
                aspectInstance => new AspectDatabaseAspectInstance(
                    GetSerializableIdForOriginalDeclaration( aspectInstance.TargetDeclaration ),
                    aspectInstance.Advice
                        .SelectMany( advice => advice.Transformations )
                        .Where( transformation => transformation.ShouldBeDisplayed() )
                        .Select(
                            transformation =>
                            {
                                var transformedDeclaration = transformation.IntroducedDeclaration ?? transformation.TargetDeclaration;

                                // ReSharper disable once RedundantSuppressNullableWarningExpression
                                return new AspectDatabaseAspectTransformation(
                                    GetSerializableIdForOriginalDeclaration( transformation.TargetDeclaration ),
                                    transformation.ToString()!,
                                    transformedDeclaration.GetClosestNamedType() is { } transformedType
                                        ? GetSerializableIdForOriginalDeclaration( transformedType )
                                        : null,
                                    transformedDeclaration.GetPrimarySyntaxTree()?.FilePath );
                            } )
                        .ToArray() ) );

        static string GetSerializableIdForOriginalDeclaration( IDeclaration declaration )
        {
            // TargetDeclaration is (usually) a transformed declaration (e.g. a constructor with an added parameter),
            // but we need the original declaration to get the correct serializable ID.
            // We can do that by going through the symbol, when it exists, which is never modified.

            if ( declaration.GetOriginalSymbol() is { } symbol )
            {
                return symbol.GetSerializableId().Id;
            }
            
            return declaration.ToSerializableId().Id;
        }

        static string? GetPredecessorFullName( IIntrospectionAspectPredecessor predecessor )
        {
            return predecessor switch
            {
                IIntrospectionAspectInstance predecessorAspect => predecessorAspect.AspectClass.Type.FullName,
                IIntrospectionFabric fabric => fabric.FullName,
                _ => null
            };
        }

#pragma warning disable IDE0300 // Don't use collection literal, since <>z__ReadOnlyArray`1 can't be deserialized here.
        var predecessorAspectInstances = aspectInstances
            .Where( aspectInstance => aspectInstance.Predecessors.Any( predecessor => GetPredecessorFullName( predecessor.Instance ) == aspectClassFullName ) )
            .Select(
                aspectInstance => new AspectDatabaseAspectInstance(
                    GetSerializableIdForOriginalDeclaration( aspectInstance.TargetDeclaration ),
                    new[]
                    {
                        new AspectDatabaseAspectTransformation(
                            GetSerializableIdForOriginalDeclaration( aspectInstance.TargetDeclaration ),
                            $"Add the '{aspectInstance.AspectClass}' aspect to '{aspectInstance.TargetDeclaration}'." )
                    } ) );
#pragma warning restore IDE0300

        return transformationAspectInstances.Concat( predecessorAspectInstances ).ToArray();
    }

    public event Action<ProjectKey> AspectClassesChanged
    {
        add => this._eventHub.AspectClassesChanged += value;
        remove => this._eventHub.AspectClassesChanged -= value;
    }

    public event Action<ProjectKey> AspectInstancesChanged
    {
        add => this._eventHub.AspectInstancesChanged += value;
        remove => this._eventHub.AspectInstancesChanged -= value;
    }
}