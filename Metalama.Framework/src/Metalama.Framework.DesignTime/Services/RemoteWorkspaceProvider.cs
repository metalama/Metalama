// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// An implementation of <see cref="WorkspaceProvider"/> that does not live in the same process as the Roslyn UI services, and uses Microsoft.CodeAnalysis.Remote.ServiceHub.
/// </summary>
internal sealed class RemoteWorkspaceProvider : WorkspaceProvider
{
    private readonly Task<Workspace> _workspace;

    public static bool TryCreate( GlobalServiceProvider serviceProvider, [NotNullWhen( true )] out RemoteWorkspaceProvider? workspaceProvider )
    {
        var logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(RemoteWorkspaceProvider) );

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

        var serviceHubAssembly = AppDomainUtility
            .GetLoadedAssemblies( a => a.FullName?.StartsWith( "Microsoft.CodeAnalysis.Remote.ServiceHub,", StringComparison.OrdinalIgnoreCase ) == true )
            .MaxByOrNull( a => a.GetName().Version );

        if ( serviceHubAssembly == null )
        {
            logger.Warning?.Log( "The assembly 'Microsoft.CodeAnalysis.Remote.ServiceHub' is not loaded." );

            workspaceProvider = null;

            return false;
        }

        var remoteWorkspaceManagerType = serviceHubAssembly.GetType( "Microsoft.CodeAnalysis.Remote.RemoteWorkspaceManager" );

        if ( remoteWorkspaceManagerType == null )
        {
            logger.Warning?.Log( "Cannot find the RemoteWorkspaceManager type." );

            workspaceProvider = null;

            return false;
        }

        // In Roslyn 4, RemoteWorkspaceManager.Default is a field.
        var remoteWorkspaceManagerDefaultField = remoteWorkspaceManagerType.GetField(
            "Default",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
        
        // In Roslyn 5, RemoteWorkspaceManager.Default is a property.
        var remoteWorkspaceManagerDefaultProperty = remoteWorkspaceManagerType.GetProperty( 
            "Default",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );

        if ( remoteWorkspaceManagerDefaultField == null && remoteWorkspaceManagerDefaultProperty == null )
        {
            logger.Warning?.Log( "Cannot find the RemoteWorkspaceManager.Default field or property." );

            workspaceProvider = null;

            return false;
        }

        var remoteWorkspaceManagerGetWorkspaceMethod = remoteWorkspaceManagerType.GetMethod(
            "GetWorkspace",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null );

        if ( remoteWorkspaceManagerGetWorkspaceMethod == null )
        {
            logger.Warning?.Log( "Cannot find the RemoteWorkspaceManager.GetWorkspace method." );

            workspaceProvider = null;

            return false;
        }

        var defaultWorkspaceManager = remoteWorkspaceManagerDefaultField?.GetValue( null ) ?? remoteWorkspaceManagerDefaultProperty?.GetValue( null );

        if ( defaultWorkspaceManager == null )
        {
            logger.Warning?.Log( " RemoteWorkspaceManager.Default returned null." );

            workspaceProvider = null;

            return false;
        }

        var workspace = (Workspace) remoteWorkspaceManagerGetWorkspaceMethod.Invoke( defaultWorkspaceManager, Array.Empty<object?>() )!;

        workspaceProvider = new RemoteWorkspaceProvider( serviceProvider, workspace );

        return true;
    }

    private RemoteWorkspaceProvider( GlobalServiceProvider serviceProvider, Workspace workspace ) : base( serviceProvider )
    {
        this._workspace = Task.FromResult( workspace );
    }

    protected override Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default ) => this._workspace;
}