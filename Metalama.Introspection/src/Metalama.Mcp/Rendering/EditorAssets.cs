// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// The files of the query editor of a served page: a trimmed build of Monaco, built by <c>Viewer/editor-build</c> and
/// embedded in the tool. The result server serves them under a path that contains <see cref="Version"/>, so that the browser
/// can cache them for as long as the tool does not change.
/// </summary>
internal static class EditorAssets
{
    private static readonly Lazy<FrozenDictionary<string, Asset>> _assets = new( Load );
    private static readonly Lazy<string> _version = new( ComputeVersion );

    public sealed record Asset( byte[] Content, string ContentType );

    /// <summary>
    /// The files, by name, with their content type. The list is fixed: nothing else is served.
    /// </summary>
    private static readonly IReadOnlyList<(string Name, string ContentType)> _files =
    [
        ("editor.js", "text/javascript; charset=utf-8"),
        ("editor.css", "text/css; charset=utf-8"),
        ("editor.worker.js", "text/javascript; charset=utf-8"),
        ("codicon.ttf", "font/ttf")
    ];

    /// <summary>
    /// Gets a hash of the files, which is part of their URL.
    /// </summary>
    public static string Version => _version.Value;

    public static bool TryGet( string name, out Asset asset ) => _assets.Value.TryGetValue( name, out asset! );

    private static FrozenDictionary<string, Asset> Load()
        => _files.ToFrozenDictionary( f => f.Name, f => new Asset( Read( f.Name ), f.ContentType ), StringComparer.Ordinal );

    private static byte[] Read( string name )
    {
        using var stream = typeof(EditorAssets).Assembly.GetManifestResourceStream( "Metalama.Mcp.Viewer.editor." + name )
                           ?? throw new InvalidOperationException( $"The editor file '{name}' is not embedded." );

        using var memory = new MemoryStream();
        stream.CopyTo( memory );

        return memory.ToArray();
    }

    private static string ComputeVersion()
    {
        using var hash = IncrementalHash.CreateHash( HashAlgorithmName.SHA256 );

        foreach ( var (name, _) in _files )
        {
            hash.AppendData( _assets.Value[name].Content );
        }

        return Convert.ToHexStringLower( hash.GetHashAndReset() ).Substring( 0, 12 );
    }
}
