// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Workspaces;

internal static class MSBuildInitializer
{
    private static readonly ILogger _logger;
    private static VisualStudioInstance? _visualStudioInstance;

    static MSBuildInitializer()
    {
        WorkspaceServices.Initialize();
        _logger = BackstageServiceFactory.ServiceProvider.GetLoggerFactory().GetLogger( "Workspace" );
    }

    public static void Initialize( string projectDirectory )
    {
        if ( MSBuildLocator.IsRegistered )
        {
            _logger.Trace?.Log( $"MSBuildLocator is already initialized." );

            return;
        }
        else if ( !MSBuildLocator.CanRegister )
        {
            throw new MSBuildInitializationException( "MSBuildLocator cannot be initialized because MSBuild assemblies are already loaded." );
        }

        if ( !Path.IsPathFullyQualified( projectDirectory ) )
        {
            throw new ArgumentOutOfRangeException( nameof(projectDirectory), "The path must be fully qualified." );
        }

        _logger.Trace?.Log(
            $"Initializing MSBuild with directory '{projectDirectory}' with {RuntimeInformation.FrameworkDescription} running on {RuntimeInformation.RuntimeIdentifier}." );

        foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
        {
            _logger.Trace?.Log( $"Loaded assembly: '{assembly}' from '{assembly.Location}'." );
        }

        var instances = new List<VisualStudioInstance>();

        foreach ( var instance in MSBuildLocator.QueryVisualStudioInstances(
                     new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = projectDirectory } ) )
        {
            _logger.Trace?.Log( $"Found {instance.Name} {instance.Version} at '{instance.MSBuildPath}'." );

            instances.Add( instance );
        }

        _visualStudioInstance = instances.OrderByDescending( i => i.Version )
            .Where( HasMatchingProcessorArchitecture )
            .FirstOrDefault();

        if ( _visualStudioInstance == null )
        {
            throw new MSBuildInitializationException(
                $"Cannot find a .NET SDK compatible with the current runtime (.NET {Environment.Version} {RuntimeInformation.RuntimeIdentifier}) for the project in '{projectDirectory}'. "
                +
                "Consider installing a compatible SDK, or run the process with a different runtime." ) { HasArchitectureMismatch = instances.Count > 0 };
        }

        _logger.Trace?.Log( $"Registering MSBuild instance {_visualStudioInstance.Name} {_visualStudioInstance.Version}." );

        MSBuildLocator.RegisterInstance( _visualStudioInstance );

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
    }

    private static bool HasMatchingProcessorArchitecture( VisualStudioInstance instance )
    {
        var versionFile = Path.Combine( instance.MSBuildPath, ".version" );

        if ( !File.Exists( versionFile ) )
        {
            _logger.Warning?.Log( $"Could not find the file '{versionFile}'." );

            return false;
        }

        var versionLines = File.ReadAllLines( versionFile );

        if ( versionFile.Length < 3 )
        {
            _logger.Warning?.Log( $"Version file {versionFile} is invalid." );

            return false;
        }

        var platform = versionLines[2].Trim();
        var expectedPlatform = RuntimeInformation.RuntimeIdentifier;

        if ( !string.Equals( platform, expectedPlatform, StringComparison.OrdinalIgnoreCase ) )
        {
            _logger.Trace?.Log( $"The SDK '{instance.MSBuildPath}' is for platform '{platform}' instead of '{expectedPlatform}'." );

            return false;
        }

        return true;
    }

    private static void OnFirstChanceException( object? sender, FirstChanceExceptionEventArgs e )
    {
        _logger.Warning?.Log( $"FirstChanceException: {e.Exception}" );
    }

    private static Assembly? OnAssemblyResolve( object? sender, ResolveEventArgs args )
    {
        _logger.Trace?.Log( $"AssemblyResolve: '{args.Name}' requested by '{args.RequestingAssembly}'." );

        return null;
    }

    private static void OnAssemblyLoad( object? sender, AssemblyLoadEventArgs args )
    {
        _logger.Trace?.Log( $"AssemblyLoad: '{args.LoadedAssembly}' from '{args.LoadedAssembly.Location}'." );
    }
}