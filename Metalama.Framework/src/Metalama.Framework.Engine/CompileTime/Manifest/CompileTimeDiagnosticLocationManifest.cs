// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
internal sealed class CompileTimeDiagnosticLocationManifest
{
    public int? FileIndex { get; set; }

    public string? FilePath { get; set; }

    public TextSpan TextSpan { get; set; }

    public LinePositionSpan? LineSpan { get; set; }

    public CompileTimeDiagnosticLocationManifest() { }

    public CompileTimeDiagnosticLocationManifest( Location location, Dictionary<string, int> sourceFilePathIndexes )
    {
        if ( location == Location.None )
        {
            this.FileIndex = -1;

            return;
        }

        // Paths of compile-time source files are always changing, so the cache uses an index as a persistent identifier for a file when possible.
        var path = location.GetLineSpan().Path.AssertNotNull();

        if ( sourceFilePathIndexes.TryGetValue( path, out var index ) )
        {
            this.FileIndex = index;
            this.TextSpan = location.SourceSpan;
        }
        else
        {
            this.FilePath = path;
            this.TextSpan = location.SourceSpan;
            this.LineSpan = location.GetLineSpan().Span;
        }
    }

    public Location ToLocation( SyntaxTree[] sourceTrees )
        => this.FileIndex switch
        {
            -1 => Location.None,
            { } index => Location.Create( sourceTrees[index], this.TextSpan ),
            null => Location.Create( this.FilePath.AssertNotNull(), this.TextSpan, this.LineSpan.AssertNotNull() )
        };
}