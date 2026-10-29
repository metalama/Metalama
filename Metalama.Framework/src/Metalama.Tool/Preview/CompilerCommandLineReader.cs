// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using SharpCrafters.Backstage.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Tool.Preview;

/// <summary>
/// Reads the command line of the compiler of a project, with the MSBuild of the .NET SDK that <c>DotNetSdkLocator</c> registered,
/// in the process.
/// </summary>
/// <remarks>
/// No type of <c>Microsoft.Build</c> may be loaded before <c>DotNetSdkLocator</c> registers the SDK, so they are used only in this
/// class, which is created after the registration.
/// </remarks>
internal sealed class CompilerCommandLineReader
{
    private readonly string _projectPath;
    private readonly IReadOnlyDictionary<string, string> _globalProperties;

    public CompilerCommandLineReader( string projectPath, string? configuration )
    {
        this._projectPath = projectPath;

        var globalProperties = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

        if ( configuration != null )
        {
            globalProperties["Configuration"] = configuration;
        }

        this._globalProperties = globalProperties;
    }

    /// <summary>
    /// Gets the target frameworks of the project, in the order of the project file.
    /// </summary>
    public IReadOnlyList<string> GetTargetFrameworks()
    {
        using var collection = new ProjectCollection();
        var project = this.CreateInstance( collection, new Dictionary<string, string>() );

        var targetFrameworks = project.GetPropertyValue( "TargetFrameworks" );

        if ( string.IsNullOrWhiteSpace( targetFrameworks ) )
        {
            targetFrameworks = project.GetPropertyValue( "TargetFramework" );
        }

        return targetFrameworks.Split( ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
            .Distinct( StringComparer.OrdinalIgnoreCase )
            .ToList();
    }

    /// <summary>
    /// Restores the project, as <c>msbuild -restore</c> does: in an evaluation of its own, which does not import the files that the
    /// restore writes, so that the next evaluation sees them.
    /// </summary>
    public void Restore()
    {
        using var collection = new ProjectCollection();

        var project = this.CreateInstance(
            collection,
            new Dictionary<string, string>
            {
                ["MSBuildRestoreSessionId"] = Guid.NewGuid().ToString( "D" ), ["ExcludeRestorePackageImports"] = "true"
            } );

        this.Build( project, "Restore" );
    }

    /// <summary>
    /// Gets the command line of the compiler for a target framework.
    /// </summary>
    public CompilerCommandLine Read( string targetFramework )
    {
        // This is what Roslyn's MSBuildWorkspace does to get the command line without compiling: the Compile target resolves the
        // references, then CoreCompile runs the Csc task, which returns its arguments instead of starting the compiler.
        // CoreCompile alone would not resolve the references. NonExistentFile is an input of CoreCompile that never exists, so
        // that CoreCompile is not skipped when the project is up to date. DesignTimeBuild is not set, because the Metalama
        // targets then add items that a real build does not have. MetalamaSkipTouchFiles keeps the Metalama targets from touching
        // the file that tells the design-time pipeline that a build started, because this run is not a build.
        using var collection = new ProjectCollection();

        var project = this.CreateInstance(
            collection,
            new Dictionary<string, string>
            {
                ["TargetFramework"] = targetFramework,
                ["SkipCompilerExecution"] = "true",
                ["ProvideCommandLineArgs"] = "true",
                ["BuildProjectReferences"] = "false",
                ["BuildingProject"] = "false",
                ["NonExistentFile"] = @"__NonExistentSubDir__\__NonExistentFile__",
                ["MetalamaSkipTouchFiles"] = "true"
            } );

        this.Build( project, "Compile" );

        return new CompilerCommandLine(
            project.GetPropertyValue( "RoslynCompilerType" ),
            project.GetPropertyValue( "RoslynTasksAssembly" ),
            project.GetItems( "CscCommandLineArgs" ).Select( i => i.EvaluatedInclude ).ToList() );
    }

    private ProjectInstance CreateInstance( ProjectCollection collection, Dictionary<string, string> additionalProperties )
    {
        var properties = new Dictionary<string, string>( this._globalProperties, StringComparer.OrdinalIgnoreCase );

        foreach ( var property in additionalProperties )
        {
            properties[property.Key] = property.Value;
        }

        try
        {
            return new ProjectInstance( this._projectPath, properties, null, collection );
        }
        catch ( Microsoft.Build.Exceptions.InvalidProjectFileException e )
        {
            throw new CommandException( $"The project '{this._projectPath}' cannot be evaluated: {e.Message}" );
        }
    }

    private void Build( ProjectInstance project, string target )
    {
        var logger = new ErrorLogger();

        if ( !project.Build( [target], [logger] ) )
        {
            var errors = logger.Errors.Count > 0 ? Environment.NewLine + string.Join( Environment.NewLine, logger.Errors ) : "";

            throw new CommandException( $"The {target} target of '{this._projectPath}' failed.{errors}" );
        }
    }

    /// <summary>
    /// Collects the errors of a build, which are reported if the build fails.
    /// </summary>
    private sealed class ErrorLogger : ILogger
    {
        public List<string> Errors { get; } = [];

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;

        public string? Parameters { get; set; }

        public void Initialize( IEventSource eventSource )
            => eventSource.ErrorRaised += ( _, e ) => this.Errors.Add( $"{e.File}({e.LineNumber},{e.ColumnNumber}): error {e.Code}: {e.Message}" );

        public void Shutdown() { }
    }
}

/// <summary>
/// The command line of the compiler of a project, as the Csc task of MSBuild gives it.
/// </summary>
/// <param name="RoslynCompilerType">The value of the <c>RoslynCompilerType</c> property, which is <c>Metalama</c> when the project uses the Metalama compiler.</param>
/// <param name="RoslynTasksAssembly">The path of the assembly of the Csc task, next to which the compiler is.</param>
/// <param name="Arguments">The arguments of the compiler, quoted as in a response file.</param>
internal sealed record CompilerCommandLine( string RoslynCompilerType, string RoslynTasksAssembly, IReadOnlyList<string> Arguments );
