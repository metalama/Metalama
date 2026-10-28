// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.Commands;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        var targetFramework = settings.Framework ?? this.GetDefaultTargetFramework( projectPath, settings );

        var tempDirectory = Path.Combine( Path.GetTempPath(), "Metalama", "Preview", Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( tempDirectory );

        try
        {
            var (compilerPath, compilerArguments) = this.GetCompilerCommandLine( projectPath, targetFramework, settings );

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
                // The directory is in the temporary directory, so it does not matter much if it cannot be deleted.
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

    private string GetDefaultTargetFramework( string projectPath, PreviewCommandSettings settings )
    {
        var properties = this.RunMSBuild( projectPath, settings, null, new Dictionary<string, string>(), ["TargetFramework", "TargetFrameworks"], [] )
            .Properties;

        var targetFramework = properties.GetValueOrDefault( "TargetFramework" );

        if ( string.IsNullOrEmpty( targetFramework ) )
        {
            targetFramework = properties.GetValueOrDefault( "TargetFrameworks" )
                ?.Split( ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
                .FirstOrDefault();
        }

        if ( string.IsNullOrEmpty( targetFramework ) )
        {
            throw new CommandException( $"Cannot determine the target framework of '{projectPath}'. Use --framework to specify it." );
        }

        return targetFramework;
    }

    private (string CompilerPath, IReadOnlyList<string> Arguments) GetCompilerCommandLine(
        string projectPath,
        string targetFramework,
        PreviewCommandSettings settings )
    {
        // This is what Roslyn's MSBuildWorkspace does to get the command line without compiling: the Compile target resolves the
        // references, then CoreCompile runs the Csc task, which returns its arguments instead of starting the compiler.
        // CoreCompile alone would not resolve the references. NonExistentFile is an input of CoreCompile that never exists, so
        // that CoreCompile is not skipped when the project is up to date. DesignTimeBuild is not set, because the Metalama
        // targets then add items that a real build does not have.
        Dictionary<string, string> properties = new()
        {
            ["TargetFramework"] = targetFramework,
            ["SkipCompilerExecution"] = "true",
            ["ProvideCommandLineArgs"] = "true",
            ["BuildProjectReferences"] = "false",
            ["BuildingProject"] = "false",
            ["NonExistentFile"] = @"__NonExistentSubDir__\__NonExistentFile__"
        };

        var result = this.RunMSBuild(
            projectPath,
            settings,
            "Compile",
            properties,
            ["RoslynCompilerType", "RoslynTasksAssembly"],
            ["CscCommandLineArgs"] );

        if ( result.Properties.GetValueOrDefault( "RoslynCompilerType" ) != "Metalama" )
        {
            throw new CommandException( $"The project '{projectPath}' does not use the Metalama compiler, so it cannot be previewed." );
        }

        var tasksAssembly = result.Properties.GetValueOrDefault( "RoslynTasksAssembly" );

        if ( string.IsNullOrEmpty( tasksAssembly ) )
        {
            throw new CommandException( "Cannot locate the Metalama compiler: the RoslynTasksAssembly property is empty." );
        }

        var compilerPath = Path.Combine( Path.GetDirectoryName( tasksAssembly )!, "bincore", "csc.dll" );

        if ( !File.Exists( compilerPath ) )
        {
            throw new CommandException( $"Cannot locate the Metalama compiler: '{compilerPath}' does not exist." );
        }

        var arguments = result.Items.GetValueOrDefault( "CscCommandLineArgs" )?.Select( i => i.Identity ).ToList() ?? [];

        if ( arguments.Count == 0 )
        {
            throw new CommandException( $"MSBuild did not return the command line of the compiler for '{projectPath}'." );
        }

        return (compilerPath, arguments);
    }

    private MSBuildResult RunMSBuild(
        string projectPath,
        PreviewCommandSettings settings,
        string? target,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyList<string> requestedProperties,
        IReadOnlyList<string> requestedItems )
    {
        var arguments = new List<string> { "msbuild", projectPath, "-nologo" };

        if ( target != null )
        {
            arguments.Add( $"-t:{target}" );

            if ( !settings.NoRestore )
            {
                arguments.Add( "-restore" );
            }
        }

        if ( settings.Configuration != null )
        {
            arguments.Add( $"-p:Configuration={settings.Configuration}" );
        }

        arguments.AddRange( properties.Select( p => $"-p:{p.Key}={p.Value}" ) );
        arguments.AddRange( requestedProperties.Select( p => $"-getProperty:{p}" ) );
        arguments.AddRange( requestedItems.Select( i => $"-getItem:{i}" ) );

        var (exitCode, output) = RunProcess( "dotnet", arguments, Path.GetDirectoryName( projectPath )! );

        // With several -getProperty or -getItem options, MSBuild writes a JSON object, but restore may write other lines before it.
        var jsonStart = output.IndexOf( '{', StringComparison.Ordinal );

        if ( exitCode != 0 || jsonStart < 0 )
        {
            this.Error.WriteLine( output );

            throw new CommandException( $"MSBuild failed on '{projectPath}'." );
        }

        return JsonSerializer.Deserialize<MSBuildResult>( output[jsonStart..], _jsonOptions )!;
    }

    private IReadOnlyList<string> RunCompiler( string compilerPath, string workingDirectory, string responseFilePath, string globalConfigPath )
    {
        // /skipanalyzers+ removes the analyzers, but keeps the source transformers. It comes after the response file so that it wins.
        var (_, output) = RunProcess(
            "dotnet",
            ["exec", compilerPath, "/noconfig", "/nologo", "/shared", $"@{responseFilePath}", "/skipanalyzers+", $"/analyzerconfig:{globalConfigPath}"],
            workingDirectory );

        // The exit code is not significant: the compiler always fails, because of the LAMA0121 error that stops it.
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

    private static (int ExitCode, string Output) RunProcess( string fileName, IEnumerable<string> arguments, string workingDirectory )
    {
        var startInfo = new ProcessStartInfo( fileName )
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach ( var argument in arguments )
        {
            startInfo.ArgumentList.Add( argument );
        }

        using var process = Process.Start( startInfo ) ?? throw new CommandException( $"Cannot start '{fileName}'." );

        var output = new StringBuilder();
        process.OutputDataReceived += ( _, e ) => AppendLine( e.Data );
        process.ErrorDataReceived += ( _, e ) => AppendLine( e.Data );
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, output.ToString());

        void AppendLine( string? line )
        {
            if ( line != null )
            {
                lock ( output )
                {
                    output.AppendLine( line );
                }
            }
        }
    }

    private sealed record MSBuildResult( Dictionary<string, string>? Properties, Dictionary<string, List<MSBuildItem>>? Items )
    {
        public Dictionary<string, string> Properties { get; } = Properties ?? [];

        public Dictionary<string, List<MSBuildItem>> Items { get; } = Items ?? [];
    }

    private sealed record MSBuildItem( string Identity );

    private sealed record PreviewManifest( List<PreviewManifestFile> Files );

    private sealed record PreviewManifestFile( string SourcePath, string? OutputPath, bool IsTransformed );
}
