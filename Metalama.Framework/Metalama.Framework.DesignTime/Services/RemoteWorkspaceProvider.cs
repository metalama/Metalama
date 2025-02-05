// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

        var remoteWorkspaceManagerDefaultField = remoteWorkspaceManagerType.GetField(
            "Default",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );

        if ( remoteWorkspaceManagerDefaultField == null )
        {
            logger.Warning?.Log( "Cannot find the RemoteWorkspaceManager.Default property." );

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

        var defaultWorkspaceManager = remoteWorkspaceManagerDefaultField.GetValue( null );

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

#pragma warning disable VSTHRD003
    protected override Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default ) => this._workspace;
}