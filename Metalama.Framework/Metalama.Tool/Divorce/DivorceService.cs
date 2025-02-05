// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LibGit2Sharp;
using Metalama.Backstage.Commands;
using Metalama.Backstage.Diagnostics;
using Metalama.Compiler;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Metalama.Tool.Divorce;

internal sealed class DivorceService
{
    private readonly ILogger _logger;
    private readonly string _path;

    public DivorceService( IServiceProvider serviceProvider, string path )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Divorce" );
        this._path = path;
    }

    public void CheckGitStatus()
    {
        var repoPath = Repository.Discover( this._path )
                       ?? throw new CommandException(
                           $"The path '{this._path}' is not part of a git repository. To execute this command anyway, use --force." );

        using var repo = new Repository( repoPath );

        if ( repo.RetrieveStatus().IsDirty )
        {
            throw new CommandException(
                $"The git repository at '{repo.Info.WorkingDirectory}' has pending changes (see git status). To execute this command anyway, use --force." );
        }
    }

    private static TransformedFilesMap ReadFileMap( string fileMapPath )
    {
        using var streamReader = File.OpenText( fileMapPath );
        using var jsonReader = new JsonTextReader( streamReader );

        return new JsonSerializer().Deserialize<TransformedFilesMap>( jsonReader )!;
    }

    private static void DisableMetalamaInProject( string projectPath )
    {
        var doc = XDocument.Load( projectPath );

        var propertyGroup = doc.Root!.Element( "PropertyGroup" );

        if ( propertyGroup == null )
        {
            propertyGroup = new XElement( "PropertyGroup" );
            doc.Root.Add( propertyGroup );
        }

        var metalamaEnabled = propertyGroup.Element( "MetalamaEnabled" );

        if ( metalamaEnabled == null )
        {
            metalamaEnabled = new XElement( "MetalamaEnabled" );
            propertyGroup.Add( metalamaEnabled );
        }

        metalamaEnabled.Value = "false";

        File.WriteAllText( projectPath, doc.ToString() );
    }

    public void PerformDivorce()
    {
        var fileMapPaths = Directory.GetFiles( this._path, TransformedFilesMap.FileName, SearchOption.AllDirectories );

        if ( !fileMapPaths.Any() )
        {
            throw new CommandException(
                $"""
                 Did not find any Metalama directories in '{this._path}'.
                 To perform divorce, first build the relevant projects while setting the MetalamaEmitCompilerTransformedFiles and MetalamaFormatOutput properties to True.
                 For example using the command: dotnet build -p:MetalamaEmitCompilerTransformedFiles=True -p:MetalamaFormatOutput=True YourSolution.sln
                 """ );
        }

        foreach ( var fileMapPath in fileMapPaths )
        {
            var fileMap = ReadFileMap( fileMapPath );

            foreach ( var mapping in fileMap.TransformedFiles )
            {
                // Relative paths seem to correspond to fake files like @@Intrinsics.cs, so ignore those.
                if ( !Path.IsPathRooted( mapping.OldPath ) )
                {
                    this._logger.Trace?.Log( $"Skipping file '{mapping.OldPath}'." );

                    continue;
                }

                if ( mapping.OldPath.Contains( "\\obj\\", StringComparison.Ordinal ) || mapping.OldPath.Contains( "/obj/", StringComparison.Ordinal ) )
                {
                    this._logger.Warning?.Log( $"File '{mapping.OldPath}' is in the obj directory, so its modifications are not going to be preserved." );
                }

                if ( !mapping.OldPath.StartsWith( this._path, StringComparison.Ordinal ) )
                {
                    this._logger.Warning?.Log( $"Skipping file '{mapping.OldPath}', because it's not inside the current directory." );

                    continue;
                }

                this._logger.Info?.Log( $"Replacing file '{mapping.OldPath}'." );

                File.Copy( mapping.NewPath, mapping.OldPath, overwrite: true );
            }
        }

        var projectPaths = Directory.GetFiles( this._path, "*.csproj", SearchOption.AllDirectories );

        if ( !projectPaths.Any() )
        {
            this._logger.Warning?.Log( $"Did not find any project files in {this._path}." );
            this._logger.Warning?.Log( "To disable Metalama in projects that still reference Metalama.Framework, set the MetalamaEnabled property to false." );
        }

        foreach ( var projectPath in projectPaths )
        {
            this._logger.Info?.Log( $"Setting property MetalamaEnabled to false in project {projectPath}." );

            DisableMetalamaInProject( projectPath );
        }
    }
}