// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Covers the transitive-manifest serialization used by the design-time cross-project inheritance path:
/// the "gate" that skips serializing a manifest with nothing to inherit (issue #1710 performance follow-up),
/// and the round-trip through both the uncompressed (in-process) and the compressed (RPC/PE) formats.
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

    private DesignTimeAspectPipelineResult Execute( TestContext testContext, TestDesignTimeAspectPipelineFactory factory, string code )
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

        var result = this.Execute( testContext, factory, _producerCode );

        Assert.True( result.HasTransitiveAspectManifestContent );

        var serialized = result.SerializedTransitiveAspectManifest;
        Assert.False( serialized.IsDefault );

        // The in-process (design-time) manifest is serialized uncompressed, so it begins with the marker byte.
        Assert.Equal( SerializationProtocol.UncompressedStreamMarker, serialized[0] );
    }

    [Fact]
    public void ProjectWithoutInheritableContent_SkipsSerialization()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = this.Execute( testContext, factory, _emptyProducerCode );

        // Gate: nothing to inherit, so the manifest is not serialized at all. The reference-construction site then
        // carries neither the live nor the serialized manifest, and the consumer skips deserialization entirely.
        Assert.False( result.HasTransitiveAspectManifestContent );
        Assert.True( result.SerializedTransitiveAspectManifest.IsDefault );
    }

    [Fact]
    public void CompressedAndUncompressedFormats_RoundTripToSameContent()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = this.Execute( testContext, factory, _producerCode );
        var serviceProvider = result.Configuration!.ServiceProvider;

        // The in-process format is uncompressed (marked); the RPC/PE format is a bare DEFLATE stream (no marker).
        var uncompressed = result.SerializedTransitiveAspectManifest;
        var compressed = result.SerializeTransitiveAspectManifestForRpc();

        Assert.Equal( SerializationProtocol.UncompressedStreamMarker, uncompressed[0] );
        Assert.NotEqual( SerializationProtocol.UncompressedStreamMarker, compressed[0] );

        // Both formats are auto-detected on read (peek of the first byte) and must decode to the same content.
        var fromUncompressed = TransitiveAspectsManifest.Deserialize( new MemoryStream( uncompressed.ToArray() ), serviceProvider, "Producer" );
        var fromCompressed = TransitiveAspectsManifest.Deserialize( new MemoryStream( compressed ), serviceProvider, "Producer" );

        Assert.Equal(
            fromUncompressed.InheritableAspectTypes.OrderBy( x => x, System.StringComparer.Ordinal ),
            fromCompressed.InheritableAspectTypes.OrderBy( x => x, System.StringComparer.Ordinal ) );

        Assert.Contains( "MyInheritableAspect", fromUncompressed.InheritableAspectTypes );
    }
}
