// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.SourceGeneration;
using System;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.SourceGeneration;

public sealed class TouchFileRendererTests
{
    [Fact]
    public void Render_ContainsGuid()
    {
        var guid = "11111111-2222-3333-4444-555555555555";
        var content = TouchFileRenderer.Render( guid );

        Assert.Contains( guid, content );
    }

    [Fact]
    public void Render_ContainsMarkerNamespaceTypeAndField()
    {
        // Guards against template/regex drift if the marker namespace, type or field constants are renamed:
        // Render must always emit each of them so the source-generator's symbol lookup and TryReadGuid's
        // regex (both keyed off TouchFileHelper.* constants) keep finding the marker.
        var content = TouchFileRenderer.Render( "G" );

        Assert.Contains( TouchFileHelper.MarkerNamespace, content );
        Assert.Contains( TouchFileHelper.MarkerTypeName, content );
        Assert.Contains( TouchFileHelper.MarkerFieldName, content );
    }

    [Fact]
    public void Render_IsDeterministic()
    {
        var c1 = TouchFileRenderer.Render( "G" );
        var c2 = TouchFileRenderer.Render( "G" );

        Assert.Equal( c1, c2 );
    }

    [Fact]
    public void RoundTrip_Render_TryReadGuid_RecoversGuid()
    {
        var guid = Guid.NewGuid().ToString();
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText( path, TouchFileRenderer.Render( guid ) );

            Assert.True( TouchFileRenderer.TryReadGuid( path, out var read ) );
            Assert.Equal( guid, read );
        }
        finally
        {
            File.Delete( path );
        }
    }

    [Fact]
    public void TryReadGuid_MalformedContent_ReturnsFalse()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText( path, "// no TouchId here" );

            Assert.False( TouchFileRenderer.TryReadGuid( path, out var read ) );
            Assert.Null( read );
        }
        finally
        {
            File.Delete( path );
        }
    }

    [Fact]
    public void TryReadGuid_MissingFile_ReturnsFalse()
    {
        var path = Path.Combine( Path.GetTempPath(), "MetalamaSourceGenerator.does-not-exist.g.cs" );

        if ( File.Exists( path ) )
        {
            File.Delete( path );
        }

        Assert.False( TouchFileRenderer.TryReadGuid( path, out var read ) );
        Assert.Null( read );
    }

    [Fact]
    public void TryReadGuid_StubFromMSBuild_RecoversEmptyGuid()
    {
        // Simulates the minimal single-line stub written by the MSBuild WriteLinesToFile call
        // before the design-time pipeline has produced its first GUID.
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(
                path,
                "namespace Metalama.Internal { [global::System.Obsolete(\"Design-time only. Do not reference.\", true)] internal static class MetalamaSourceGeneratorMarker { public const string TouchId = \"\"; } }\n" );

            Assert.True( TouchFileRenderer.TryReadGuid( path, out var read ) );
            Assert.Equal( "", read );
        }
        finally
        {
            File.Delete( path );
        }
    }
}
