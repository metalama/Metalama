// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Diagnostics;
using Microsoft.Build.Locator;
using SharpCrafters.Aspects.Introspection.Query.Projects;
using SharpCrafters.Backstage.Diagnostics;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Registers the MSBuild of a .NET SDK before a workspace loads its projects.
/// </summary>
/// <remarks>
/// The choice of the .NET SDK is shared with the <c>introspect</c> commands of the tools: it is <see cref="DotNetSdkLocator"/> of
/// <c>SharpCrafters.Aspects.Introspection.Query</c>, which chooses the highest SDK that the runtime of the process can load. This
/// class adds the logging of the assemblies that MSBuild loads.
/// </remarks>
internal static class MSBuildInitializer
{
    private static readonly ILogger _logger;

    static MSBuildInitializer()
    {
        WorkspaceServices.Initialize();
        _logger = BackstageServiceFactoryInitializer.ServiceProvider.GetLoggerFactory().GetLogger( "Workspace" );
    }

    public static void Initialize( string projectDirectory )
    {
        if ( MSBuildLocator.IsRegistered )
        {
            _logger.Trace?.Log( $"MSBuildLocator is already initialized." );

            return;
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

        var registration = DotNetSdkLocator.EnsureRegistered();

        if ( registration.Sdk == null )
        {
            throw new MSBuildInitializationException( $"Cannot initialize MSBuild for the project in '{projectDirectory}'. {registration.Failure}" )
            {
                HasArchitectureMismatch = registration.HasIncompatibleSdks
            };
        }

        _logger.Trace?.Log( $"Registered the MSBuild of the .NET SDK {registration.Sdk.Version} in '{registration.Sdk.Directory}'." );

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
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
