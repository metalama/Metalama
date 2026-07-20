// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Covers the transitive-manifest serialization used by the design-time cross-project inheritance path: the
/// condition on which a caller skips carrying a manifest with nothing to inherit (issue #1710 performance
/// follow-up), and the round-trip through both the uncompressed and the legacy compressed formats.
/// </summary>
public sealed class TransitiveManifestSerializationTests : UnitTestClass
{
    public TransitiveManifestSerializationTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// An <c>[Inheritable]</c> aspect applied to a class exports an inheritable aspect instance, so this project
    /// has transitive content a referencing project could inherit.
    /// </summary>
    private const string _producerCode = """
                                         using Metalama.Framework.Aspects;
                                         using Metalama.Framework.Code;

                                         [Inheritable]
                                         public class MyInheritableAspect : TypeAspect { }

                                         [MyInheritableAspect]
                                         public class Base { }
                                         """;

    /// <summary>
    /// A non-inheritable aspect: it does real design-time work but exports nothing to inherit (no inheritable
    /// aspect, option, annotation, or validator), so its transitive manifest is empty.
    /// </summary>
    private const string _emptyProducerCode = """
                                              using Metalama.Framework.Aspects;
                                              using Metalama.Framework.Code;

                                              public class MyLocalAspect : TypeAspect { }

                                              [MyLocalAspect]
                                              public class Local { }
                                              """;

    private static DesignTimeAspectPipelineResult Execute( TestContext testContext, TestDesignTimeAspectPipelineFactory factory, string code )
    {
        var compilation = testContext.CreateCSharpCompilation( code );
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

        return result.Result;
    }

    [Fact]
    public void ProjectWithInheritableContent_ProducesSerializedManifest()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = Execute( testContext, factory, _producerCode );

        Assert.True( result.HasTransitiveAspectManifestContent );

        var serialized = result.SerializedTransitiveAspectManifestWithoutValidators;
        Assert.NotEmpty( serialized.Bytes );

        // The in-process (design-time) manifest is serialized uncompressed, so it begins with the marker byte.
        Assert.Equal( SerializationProtocol.UncompressedStreamMarker, serialized.Bytes[0] );
    }

    [Fact]
    public void ProjectWithoutInheritableContent_ReportsNothingToInherit()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = Execute( testContext, factory, _emptyProducerCode );

        // The gate itself lives on the caller: DesignTimeAspectPipeline builds a DesignTimeProjectReference carrying
        // neither the live nor the serialized manifest when this is false, so the consumer skips deserialization
        // entirely. The property below would serialize on demand; nobody asks it to.
        Assert.False( result.HasTransitiveAspectManifestContent );
    }

    /// <summary>
    /// The hash identifies the manifest by content, so two runs that export the same surface agree on it. This is
    /// what lets a consumer skip re-deserializing after a producer edit that did not touch the exported surface.
    /// </summary>
    [Fact]
    public void SerializedManifest_HashesItsContent()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var serialized = Execute( testContext, factory, _producerCode ).SerializedTransitiveAspectManifestWithoutValidators;

        Assert.NotEqual( 0, serialized.Hash );

        // Recomputing from the same bytes yields the same hash, and the value compares equal.
        var recreated = SerializedTransitiveAspectManifest.Create( serialized.Bytes );

        Assert.Equal( serialized.Hash, recreated.Hash );
        Assert.Equal( serialized, recreated );
    }

    /// <summary>
    /// Equality is by hash alone: equal content compares equal because it hashes equally, and differing content
    /// compares different because it does not. The bytes are deliberately not consulted, so a hash equality is
    /// taken as an identity; see the remarks on <see cref="SerializedTransitiveAspectManifest"/>.
    /// </summary>
    [Fact]
    public void Equality_IsByHashAlone()
    {
        var a = SerializedTransitiveAspectManifest.Create( ImmutableArray.Create<byte>( 1, 2, 3 ) );
        var b = SerializedTransitiveAspectManifest.Create( ImmutableArray.Create<byte>( 1, 2, 3 ) );
        var c = SerializedTransitiveAspectManifest.Create( ImmutableArray.Create<byte>( 1, 2, 4 ) );

        // Equal content held in two distinct backing arrays (ImmutableArray's == compares those by reference), so
        // this also pins that equality is not merely by reference.
        Assert.False( a.Bytes == b.Bytes );
        Assert.Equal( a, b );
        Assert.True( a == b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );

        Assert.NotEqual( a, c );
        Assert.True( a != c );

        Assert.NotNull( a );
    }

    [Fact]
    public void CompressedAndUncompressedFormats_RoundTripToSameContent()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = Execute( testContext, factory, _producerCode );
        var serviceProvider = result.Configuration!.ServiceProvider;

        // Both design-time manifests are uncompressed (marked). The compressed form is written only by ToResource
        // now, but a reader must still accept it: this project can reference an older one, whose manifest predates
        // the marker and arrives as a bare DEFLATE stream. That legacy shape is what `compressed` stands in for.
        var uncompressed = result.SerializedTransitiveAspectManifestWithoutValidators;
        var crossVersion = result.SerializedTransitiveAspectManifestWithValidators;
        var compressed = result.LiveTransitiveAspectManifest.ToBytes( serviceProvider, compress: true );

        Assert.Equal( SerializationProtocol.UncompressedStreamMarker, uncompressed.Bytes[0] );
        Assert.Equal( SerializationProtocol.UncompressedStreamMarker, crossVersion.Bytes[0] );
        Assert.NotEqual( SerializationProtocol.UncompressedStreamMarker, compressed[0] );

        // Both formats are auto-detected on read (peek of the first byte) and must decode to the same content.
        var fromUncompressed = TransitiveAspectsManifest.Deserialize( new MemoryStream( uncompressed.Bytes.ToArray() ), serviceProvider, "Producer" );
        var fromCompressed = TransitiveAspectsManifest.Deserialize( new MemoryStream( compressed ), serviceProvider, "Producer" );

        Assert.Equal(
            fromUncompressed.InheritableAspectTypes.OrderBy( x => x, StringComparer.Ordinal ),
            fromCompressed.InheritableAspectTypes.OrderBy( x => x, StringComparer.Ordinal ) );

        Assert.Contains( "MyInheritableAspect", fromUncompressed.InheritableAspectTypes );
    }
}