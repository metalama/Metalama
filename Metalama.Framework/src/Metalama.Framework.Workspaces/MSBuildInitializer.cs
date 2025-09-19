// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine;
using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
        
        // We choose the .NET SDK using `dotnet --list-sdks` because MSBuildLocator does not find .NET SDKs installed 
        // using dotnet-installer.ps1 on Docker.
        if ( !ToolInvocationHelper.InvokeTool(
                _logger,
                "dotnet",
                "--list-sdks",
                Environment.CurrentDirectory,
                out var exitCode,
                out var sdkListString ) )
        {
            throw new MSBuildInitializationException( $"`dotnet --list-sdks` failed with exit code {exitCode}." + Environment.NewLine + sdkListString );
        }

        var parseSdkList = new Regex( @"^(?<version>[0-9]+(?:\.[0-9]+)*(?:-[A-Za-z0-9\.]+)?)\s+\[(?<directory>[^\]]+)\]$", RegexOptions.Compiled );

        var sdks = sdkListString.Split( '\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
            .Select( x => parseSdkList.Match( x ) )
            .Where( x => x.Success )
            .Select( x => (Version: x.Groups["version"].Value, Directory: Path.Combine( x.Groups["directory"].Value, x.Groups["version"].Value )) )
            .Select( x =>
                         (IsParsed: Version.TryParse( x.Version.Split( '-' )[0], out var parsedVersion ), ParsedVersion: parsedVersion, x.Version,
                          x.Directory) )
            .Where( x => x.IsParsed )
            .ToReadOnlyList();

        var highestSdk = sdks
            .Where( i => i.ParsedVersion.Major <= Environment.Version.Major )
            .OrderByDescending( i => i.ParsedVersion )
            .ThenBy( i => i.Version )
            .FirstOrDefault( x => HasMatchingProcessorArchitecture( x.Directory ) );

        if ( highestSdk.Directory == null )
        {
            throw new MSBuildInitializationException(
                $"Cannot find a .NET SDK compatible with the current runtime (.NET {Environment.Version} {RuntimeInformation.RuntimeIdentifier}) for the project in '{projectDirectory}'. "
                +$"Found the following SDKs: {string.Join( ",", sdks.Select( x => x.Version ) )}, but they not compatible with the current runtime." +
                "Consider installing a compatible SDK, or run the process with a different runtime." ) { HasArchitectureMismatch = sdks.Count > 0 };
        }

        var constructor = typeof(VisualStudioInstance).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            [
                typeof(string), typeof(string), typeof(Version),
                typeof(DiscoveryType)
            ] );

        if ( constructor == null )
        {
            throw new AssertionFailedException( $"Cannot find the internal constructor for {nameof(VisualStudioInstance)}." );
        }

        _visualStudioInstance = (VisualStudioInstance) constructor.Invoke(
        [
            $".NET SDK {highestSdk.Version}",
            highestSdk.Directory,
            highestSdk.ParsedVersion,
            DiscoveryType.DotNetSdk
        ] );

        _logger.Trace?.Log( $"Registering MSBuild instance {_visualStudioInstance.Name} {_visualStudioInstance.Version}." );

        MSBuildLocator.RegisterInstance( _visualStudioInstance );

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
    }

    private static bool HasMatchingProcessorArchitecture( string directory )
    {
        var versionFile = Path.Combine( directory, ".version" );

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
            _logger.Trace?.Log( $"The SDK '{directory}' is for platform '{platform}' instead of '{expectedPlatform}'." );

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