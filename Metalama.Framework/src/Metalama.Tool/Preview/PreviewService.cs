// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Aspects.Introspection.Query.Projects;
using SharpCrafters.Backstage.Commands;
using SharpCrafters.Backstage.Diagnostics;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.Maintenance;
using SharpCrafters.Backstage.Utilities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Metalama.Tool.Preview;

/// <summary>
/// Implements <c>metalama preview</c>. The command uses MSBuild only to get the command line of the compiler, then runs the
/// Metalama compiler of the project itself, so that the version of Metalama that transforms the code is the one of the project.
/// The compiler runs the aspects on the whole project, but links and formats only the requested files, writes them to a
/// temporary directory with a <c>preview.json</c> manifest, and then stops with the LAMA0121 error, before running the source
/// generators and the analyzers and before emitting the assembly.
/// </summary>
internal sealed class PreviewService
{
    // Reported by the compiler when the preview is complete. It is an error only to make the compiler stop.
    private const string _previewCompletedDiagnosticId = "LAMA0121";

    private const string _manifestFileName = "preview.json";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ExtendedCommandContext _context;

    public PreviewService( ExtendedCommandContext context )
    {
        this._context = context;
    }

    private IAnsiConsole Error => this._context.Console.Error;

    public void Execute( PreviewCommandSettings settings )
    {
        if ( settings.Files.Length == 0 )
        {
            throw new CommandException( "At least one file must be specified." );
        }

        var files = settings.Files.Select( Path.GetFullPath ).ToArray();

        foreach ( var file in files )
        {
            if ( !File.Exists( file ) )
            {
                throw new CommandException( $"The file '{file}' does not exist." );
            }
        }

        var projectPath = settings.Project != null ? Path.GetFullPath( settings.Project ) : FindProject( files[0] );
        var projectDirectory = Path.GetDirectoryName( projectPath )!;

        // MSBuild runs in the process, so the MSBuild of a .NET SDK must be registered before any of its types is loaded.
        var registration = DotNetSdkLocator.EnsureRegistered();

        if ( registration.Failure != null )
        {
            throw new CommandException( registration.Failure );
        }

        var (compilerPath, compilerArguments) = GetCompilerCommandLine( projectPath, settings );

        // Each preview gets a directory of its own, which is deleted at the end, and otherwise by the clean-up of Backstage.
        var tempDirectory = this._context.ServiceProvider.GetRequiredBackstageService<ITempFileManager>()
            .GetTempDirectory( "Preview", CleanUpStrategy.Always, Guid.NewGuid().ToString( "N" ) );

        try
        {
            // The compiler options of the preview are passed in a global analyzer configuration file of their own, rather than as
            // MSBuild properties, so that the configuration file that MSBuild generates in obj, which is an input of the build,
            // is not changed. Its global_level makes these values win over the empty ones of that file.
            var filesListPath = Path.Combine( tempDirectory, "files.txt" );
            File.WriteAllLines( filesListPath, files );

            var outputDirectory = Path.Combine( tempDirectory, "output" );
            var globalConfigPath = Path.Combine( tempDirectory, "preview.globalconfig" );

            File.WriteAllText(
                globalConfigPath,
                $"""
                 is_global = true
                 global_level = 1000
                 build_property.MetalamaPreviewFiles = {filesListPath}
                 build_property.MetalamaPreviewOutputPath = {outputDirectory}
                 """ );

            var responseFilePath = Path.Combine( tempDirectory, "compiler.rsp" );

            // The arguments are already quoted as the compiler expects in a response file.
            File.WriteAllLines( responseFilePath, compilerArguments.Where( IsKeptCompilerArgument ) );

            var compilerOutput = this.RunCompiler( compilerPath, projectDirectory, responseFilePath, globalConfigPath );

            var manifestPath = Path.Combine( outputDirectory, _manifestFileName );

            // Report the diagnostics of the compiler, except the one that stops it on purpose.
            var hasErrors = false;

            foreach ( var line in compilerOutput.Where( l => !string.IsNullOrWhiteSpace( l ) ) )
            {
                if ( line.Contains( _previewCompletedDiagnosticId, StringComparison.Ordinal ) )
                {
                    continue;
                }

                if ( line.Contains( ": error ", StringComparison.Ordinal ) )
                {
                    hasErrors = true;
                }

                this.Error.WriteLine( line );
            }

            if ( !File.Exists( manifestPath ) )
            {
                throw new CommandException( "The compiler did not produce the preview. See the errors above." );
            }

            this.WriteOutput( manifestPath, settings.Output );

            if ( hasErrors )
            {
                throw new CommandException( "The compiler reported errors." );
            }
        }
        finally
        {
            try
            {
                Directory.Delete( tempDirectory, true );
            }
            catch ( IOException )
            {
                // The clean-up of Backstage deletes the directory later.
            }
        }
    }

    private static string FindProject( string file )
    {
        for ( var directory = Path.GetDirectoryName( file ); directory != null; directory = Path.GetDirectoryName( directory ) )
        {
            var projects = Directory.GetFiles( directory, "*.csproj" );

            switch ( projects.Length )
            {
                case 1:
                    return projects[0];

                case > 1:
                    throw new CommandException( $"The directory '{directory}' contains several projects. Use --project to choose one." );
            }
        }

        throw new CommandException( $"No project was found for '{file}'. Use --project to specify it." );
    }

    // Not inlined, so that the types of MSBuild, which CompilerCommandLineReader uses, are not loaded before the SDK is registered.
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static (string CompilerPath, IReadOnlyList<string> Arguments) GetCompilerCommandLine( string projectPath, PreviewCommandSettings settings )
    {
        var reader = new CompilerCommandLineReader( projectPath, settings.Configuration );

        if ( !settings.NoRestore )
        {
            reader.Restore();
        }

        var targetFramework = settings.Framework
                              ?? reader.GetDefaultTargetFramework()
                              ?? throw new CommandException(
                                  $"Cannot determine the target framework of '{projectPath}'. Use --framework to specify it." );

        var commandLine = reader.Read( targetFramework );

        if ( commandLine.RoslynCompilerType != "Metalama" )
        {
            throw new CommandException( $"The project '{projectPath}' does not use the Metalama compiler, so it cannot be previewed." );
        }

        if ( string.IsNullOrEmpty( commandLine.RoslynTasksAssembly ) )
        {
            throw new CommandException( "Cannot locate the Metalama compiler: the RoslynTasksAssembly property is empty." );
        }

        var compilerPath = Path.Combine( Path.GetDirectoryName( commandLine.RoslynTasksAssembly )!, "bincore", "csc.dll" );

        if ( !File.Exists( compilerPath ) )
        {
            throw new CommandException( $"Cannot locate the Metalama compiler: '{compilerPath}' does not exist." );
        }

        if ( commandLine.Arguments.Count == 0 )
        {
            throw new CommandException( $"MSBuild did not return the command line of the compiler for '{projectPath}'." );
        }

        return (compilerPath, commandLine.Arguments);
    }

    private IReadOnlyList<string> RunCompiler( string compilerPath, string workingDirectory, string responseFilePath, string globalConfigPath )
    {
        // /shared runs the compilation in the compiler server, which keeps Metalama loaded between previews.
        // /skipanalyzers+ removes the analyzers, but keeps the source transformers. It comes after the response file so that it wins.
        var commandLine =
            $"exec \"{compilerPath}\" /noconfig /nologo /shared \"@{responseFilePath}\" /skipanalyzers+ \"/analyzerconfig:{globalConfigPath}\"";

        // The command line is logged, not written to the console, and the default options keep the environment variables that
        // the registration of MSBuild set in this process away from the compiler.
        var logger = this._context.ServiceProvider.GetLoggerFactory().GetLogger( "Preview" );

        // The exit code and the result are not significant: the compiler always fails, because of the LAMA0121 error that stops it.
        ToolInvocationHelper.InvokeTool( logger, "dotnet", commandLine, workingDirectory, out _, out var output, ToolInvocationOptions.Default with { Silent = true } );

        return output.Split( ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries );
    }

    private void WriteOutput( string manifestPath, string? outputDirectory )
    {
        var manifest = JsonSerializer.Deserialize<PreviewManifest>( File.ReadAllText( manifestPath ), _jsonOptions )!;

        if ( outputDirectory != null )
        {
            Directory.CreateDirectory( outputDirectory );
        }

        var usedNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

        foreach ( var file in manifest.Files )
        {
            if ( file.OutputPath == null )
            {
                throw new CommandException( $"The file '{file.SourcePath}' is not part of the compilation of the project." );
            }

            if ( outputDirectory == null )
            {
                var stdout = Console.Out;
                stdout.WriteLine( $"// ===== {file.SourcePath}{(file.IsTransformed ? "" : " (not transformed)")} =====" );
                stdout.WriteLine( File.ReadAllText( file.OutputPath ) );
            }
            else
            {
                var name = Path.GetFileName( file.SourcePath );

                for ( var i = 2; !usedNames.Add( name ); i++ )
                {
                    name = $"{Path.GetFileNameWithoutExtension( file.SourcePath )}_{i}{Path.GetExtension( file.SourcePath )}";
                }

                File.Copy( file.OutputPath, Path.Combine( outputDirectory, name ), true );
                this._context.Console.WriteMessage( $"{file.SourcePath} -> {Path.Combine( outputDirectory, name )}" );
            }
        }
    }

    // The compiler stops before emitting, so the output options do not matter, but the documentation file, the reference assembly and
    // the error log are removed to be sure that nothing of the build is overwritten. /out is kept, because it names the assembly.
    // /noconfig is passed on the command line, where it is honored, instead of in the response file, where it is not.
    private static bool IsKeptCompilerArgument( string argument )
        => !argument.StartsWith( "/refout:", StringComparison.OrdinalIgnoreCase )
           && !argument.StartsWith( "/doc:", StringComparison.OrdinalIgnoreCase )
           && !argument.StartsWith( "/errorlog:", StringComparison.OrdinalIgnoreCase )
           && !argument.Equals( "/noconfig", StringComparison.OrdinalIgnoreCase );

    private sealed record PreviewManifest( List<PreviewManifestFile> Files );

    private sealed record PreviewManifestFile( string SourcePath, string? OutputPath, bool IsTransformed );
}
