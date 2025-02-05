// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.Contracts.AspectExplorer;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;

internal sealed class AspectDatabase : IAspectDatabaseService2, IDisposable
{
    private readonly ServiceHubRpcService _serviceHub;

    public AspectDatabase( GlobalServiceProvider serviceProvider )
    {
        this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;

        this._serviceHub.EventReceived += this.OnServiceHubEventReceived;
    }

    private void OnServiceHubEventReceived( RpcEventData eventData )
    {
        if ( eventData.Category == nameof(IAspectExplorerRpcApi) )
        {
            switch ( eventData )
            {
                case AspectClassesChangedEventData aspectClassesChangedEventData:
                    this.AspectClassesChanged?.Invoke( aspectClassesChangedEventData.ProjectKey.AssemblyName );

                    break;

                case AspectInstancesChangedEventData aspectInstancesChangedEventData:
                    this.AspectInstancesChanged?.Invoke( aspectInstancesChangedEventData.ProjectKey.AssemblyName );

                    break;
            }
        }
    }

    public async Task<IEnumerable<INamedTypeSymbol>> GetAspectClassesAsync( Compilation compilation, CancellationToken cancellationToken )
    {
        var projectKey = compilation.GetProjectKey();

        if ( !projectKey.IsMetalamaEnabled )
        {
            return [];
        }

        var api = await this._serviceHub.GetApiForProjectAsync<IAspectExplorerRpcApi>( projectKey, cancellationToken );

        var aspectClasses = await api.GetAspectClassesAsync( projectKey, cancellationToken );

        var typeIdResolver = compilation.GetCompilationContext().SerializableTypeIdResolver;

        INamedTypeSymbol? ResolveOrNull( string serializableTypeId )
        {
            typeIdResolver.TryResolveId( new SerializableTypeId( serializableTypeId ), out var symbol );

            return (INamedTypeSymbol?) symbol;
        }

        // Resolving can fail when the pipeline is paused and the aspect class does not exist in the modified compilation.
        return aspectClasses.Select( ResolveOrNull ).WhereNotNull().ToArray();
    }

#pragma warning disable CS0612 // Type or member is obsolete
    private static AspectExplorerAspectInstance ToVersion1( IAspectExplorerAspectInstance aspectInstance )
        => new()
        {
            TargetDeclaration = aspectInstance.TargetDeclaration,
            TargetDeclarationKind = aspectInstance.TargetDeclarationKind,
            Transformations = aspectInstance.Transformations.Select( ToVersion1 ).ToArray()
        };

    private static AspectExplorerAspectTransformation ToVersion1( IAspectExplorerAspectTransformation transformation )
        => new()
        {
            TargetDeclaration = transformation.TargetDeclaration,
            TargetDeclarationKind = transformation.TargetDeclarationKind,
            Description = transformation.Description
        };

    public async Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<AspectExplorerAspectInstance>[] result,
        CancellationToken cancellationToken )
    {
        var version2Result = new IEnumerable<IAspectExplorerAspectInstance>[1];

        await this.GetAspectInstancesAsync( compilation, aspectClass, version2Result, cancellationToken );

        result[0] = version2Result[0].Select( ToVersion1 ).ToArray();
    }
#pragma warning restore CS0612

    public async Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<IAspectExplorerAspectInstance>[] result,
        CancellationToken cancellationToken )
    {
        var projectKey = compilation.GetProjectKey();

        if ( !projectKey.IsMetalamaEnabled )
        {
            result[0] = [];

            return;
        }

        var api = await this._serviceHub.GetApiForProjectAsync<IAspectExplorerRpcApi>( projectKey, cancellationToken );

        var aspectInstances = await api.GetAspectInstancesAsync(
            projectKey,
            aspectClass.ContainingAssembly.Name,
            aspectClass.GetSerializableTypeId().Id,
            cancellationToken );

        result[0] = GetAspectInstances().ToArray();

        IEnumerable<IAspectExplorerAspectInstance> GetAspectInstances()
        {
            foreach ( var aspectInstance in aspectInstances )
            {
                var targetDeclaration = ResolveToSymbol( aspectInstance.TargetDeclarationId, out var targetDeclarationKind );

                if ( targetDeclaration is null )
                {
                    continue;
                }

                yield return new AspectExplorerAspectInstance2( targetDeclaration, targetDeclarationKind, GetTransformations( aspectInstance ).ToArray() );
            }
        }

        IEnumerable<IAspectExplorerAspectTransformation> GetTransformations( AspectDatabaseAspectInstance aspectInstance )
        {
            foreach ( var transformation in aspectInstance.Transformations )
            {
                var targetDeclaration = ResolveToSymbol( transformation.TargetDeclarationId, out var targetDeclarationKind );

                if ( targetDeclaration is null )
                {
                    continue;
                }

                var transformedDeclaration = ResolveToSymbol( transformation.TransformedDeclarationId, out var transformedDeclarationKind );
                Invariant.Assert( transformedDeclarationKind == default );

                yield return new AspectExplorerAspectTransformation2(
                    targetDeclaration,
                    targetDeclarationKind,
                    transformation.Description,
                    transformedDeclaration,
                    transformation.FilePath );
            }
        }

        ISymbol? ResolveToSymbol( string? id, out AspectExplorerDeclarationKind kind )
        {
            if ( id is null )
            {
                kind = AspectExplorerDeclarationKind.Default;

                return null;
            }

            var symbol = new SerializableDeclarationId( id ).ResolveToSymbolOrNull( compilation.GetCompilationContext(), out var isReturnParameter );

            kind = isReturnParameter ? AspectExplorerDeclarationKind.ReturnParameter : AspectExplorerDeclarationKind.Default;

            return symbol;
        }
    }

    public event Action<string>? AspectClassesChanged;

    public event Action<string>? AspectInstancesChanged;

    public void Dispose()
    {
        this._serviceHub.EventReceived -= this.OnServiceHubEventReceived;
    }

    private sealed record AspectExplorerAspectInstance2(
        ISymbol TargetDeclaration,
        AspectExplorerDeclarationKind TargetDeclarationKind,
        IAspectExplorerAspectTransformation[] Transformations ) : IAspectExplorerAspectInstance;

    private sealed record AspectExplorerAspectTransformation2(
        ISymbol TargetDeclaration,
        AspectExplorerDeclarationKind TargetDeclarationKind,
        string Description,
        ISymbol? TransformedDeclaration,
        string? FilePath ) : IAspectExplorerAspectTransformation;
}