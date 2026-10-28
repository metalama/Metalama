// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Metalama.Framework.Engine.Pipeline.CompileTime;

/// <summary>
/// Implements the compile-time side of the <c>metalama preview</c> command. The command runs the compiler with
/// <see cref="IProjectOptions.PreviewFilesListPath"/> set. The pipeline then runs the aspects on the whole project, but links
/// and formats only the previewed files, writes them with <see cref="Write"/>, and stops the compiler.
/// </summary>
/// <remarks>
/// The output directory contains a <c>preview.json</c> manifest listing, for each requested file, the file that holds its
/// transformed code. The command reads this manifest, so its format is a contract between the command and the engine.
/// </remarks>
internal sealed class CompileTimePreview
{
    public const string ManifestFileName = "preview.json";

    // Paths are compared without regard to case, because the command and the compiler can spell the same path differently on Windows.
    private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly string _outputPath;

    // The requested paths, normalized, each with the path of the syntax tree it maps to, or null if no tree of the compilation has this path.
    private readonly ImmutableArray<(string RequestedPath, string? SyntaxTreePath)> _files;

    private CompileTimePreview( string outputPath, ImmutableArray<(string RequestedPath, string? SyntaxTreePath)> files )
    {
        this._outputPath = outputPath;
        this._files = files;

        this.ObservedSyntaxTreePaths = files.Select( f => f.SyntaxTreePath )
            .WhereNotNull()
            .ToImmutableHashSet( _pathComparer );
    }

    /// <summary>
    /// Gets the paths of the syntax trees that must be linked and formatted, as spelled by the syntax trees.
    /// </summary>
    public ImmutableHashSet<string> ObservedSyntaxTreePaths { get; }

    /// <summary>
    /// Creates a <see cref="CompileTimePreview"/> if the project options request a preview, or returns <c>null</c>.
    /// </summary>
    public static CompileTimePreview? Create( IProjectOptions projectOptions, Compilation compilation )
    {
        var listPath = projectOptions.PreviewFilesListPath;

        if ( listPath == null )
        {
            return null;
        }

        var outputPath = projectOptions.PreviewOutputPath ?? Path.GetDirectoryName( Path.GetFullPath( listPath ) )!;

        var syntaxTreePathsByFullPath = new Dictionary<string, string>( _pathComparer );

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            if ( TryGetFullPath( syntaxTree.FilePath, out var fullPath ) )
            {
                syntaxTreePathsByFullPath[fullPath] = syntaxTree.FilePath;
            }
        }

        var files = File.ReadAllLines( listPath )
            .SelectAsArray( line => line.Trim() )
            .Where( line => line.Length > 0 )
            .Select(
                line =>
                {
                    var requestedPath = TryGetFullPath( line, out var fullPath ) ? fullPath : line;

                    return (requestedPath, syntaxTreePathsByFullPath.TryGetValue( requestedPath, out var syntaxTreePath ) ? syntaxTreePath : null);
                } )
            .ToImmutableArray();

        return new CompileTimePreview( outputPath, files );
    }

    private static bool TryGetFullPath( string path, out string fullPath )
    {
        if ( string.IsNullOrEmpty( path ) )
        {
            fullPath = "";

            return false;
        }

        try
        {
            fullPath = Path.GetFullPath( path );

            return true;
        }
        catch ( Exception e ) when ( e is ArgumentException or NotSupportedException or PathTooLongException )
        {
            fullPath = "";

            return false;
        }
    }

    public bool IsObserved( SyntaxTree syntaxTree ) => this.ObservedSyntaxTreePaths.Contains( syntaxTree.FilePath );

    /// <summary>
    /// Writes the previewed files and the manifest into the output directory.
    /// </summary>
    /// <param name="compilation">The transformed compilation.</param>
    /// <returns>The path of the output directory.</returns>
    public string Write( PartialCompilation compilation )
    {
        Directory.CreateDirectory( this._outputPath );

        var transformedSyntaxTrees = compilation.Compilation.SyntaxTrees
            .Where( this.IsObserved )
            .ToDictionary( t => t.FilePath, _pathComparer );

        var initialSyntaxTrees = compilation.InitialCompilation.SyntaxTrees
            .Where( this.IsObserved )
            .ToDictionary( t => t.FilePath, _pathComparer );

        var manifestFiles = new List<PreviewManifestFile>( this._files.Length );

        for ( var i = 0; i < this._files.Length; i++ )
        {
            var (requestedPath, syntaxTreePath) = this._files[i];

            if ( syntaxTreePath == null || !transformedSyntaxTrees.TryGetValue( syntaxTreePath, out var transformedSyntaxTree ) )
            {
                manifestFiles.Add( new PreviewManifestFile( requestedPath, null, false ) );

                continue;
            }

            var text = transformedSyntaxTree.GetText();

            var isTransformed = !initialSyntaxTrees.TryGetValue( syntaxTreePath, out var initialSyntaxTree )
                                || !initialSyntaxTree.GetText().ContentEquals( text );

            // Each file gets its own directory, so that two requested files with the same name do not collide.
            var outputFilePath = Path.Combine( this._outputPath, i.ToString( System.Globalization.CultureInfo.InvariantCulture ), Path.GetFileName( requestedPath ) );
            Directory.CreateDirectory( Path.GetDirectoryName( outputFilePath )! );

            using ( var writer = new StreamWriter( outputFilePath, false, text.Encoding ?? new UTF8Encoding( false ) ) )
            {
                text.Write( writer );
            }

            manifestFiles.Add( new PreviewManifestFile( requestedPath, outputFilePath, isTransformed ) );
        }

        var manifest = new PreviewManifest( manifestFiles );
        var manifestJson = JsonSerializer.Serialize( manifest, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase } );
        File.WriteAllText( Path.Combine( this._outputPath, ManifestFileName ), manifestJson );

        return this._outputPath;
    }

    /// <param name="Files">The requested files, in the order of the request.</param>
    private sealed record PreviewManifest( IReadOnlyList<PreviewManifestFile> Files );

    /// <param name="SourcePath">The full path of the requested file.</param>
    /// <param name="OutputPath">The path of the file holding its transformed code, or <c>null</c> if the file is not part of the compilation.</param>
    /// <param name="IsTransformed">A value indicating whether Metalama changed the file.</param>
    private sealed record PreviewManifestFile( string SourcePath, string? OutputPath, bool IsTransformed );
}
