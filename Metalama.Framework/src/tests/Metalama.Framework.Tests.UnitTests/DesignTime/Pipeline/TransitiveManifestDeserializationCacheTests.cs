// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Options;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Covers <see cref="TransitiveManifestDeserializationCache"/>. These assert on whether the deserialization actually
/// ran, by counting how often the factory is invoked, rather than on the returned value: a cache that always
/// deserialized would still return a correct manifest, so only the call count distinguishes a hit from a miss.
/// </summary>
public sealed class TransitiveManifestDeserializationCacheTests : UnitTestClass
{
    public TransitiveManifestDeserializationCacheTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private static readonly AssemblyIdentity _producerA = new( "ProducerA" );
    private static readonly AssemblyIdentity _producerB = new( "ProducerB" );

    private static SerializedTransitiveAspectManifest CreateManifest( params byte[] bytes )
        => SerializedTransitiveAspectManifest.Create( ImmutableArray.Create( bytes ) );

    /// <summary>
    /// A counting stand-in for the deserialization, which the cache is supposed to call only on a miss.
    /// </summary>
    private sealed class CountingDeserializer
    {
        public int CallCount { get; private set; }

        public ITransitiveAspectsManifest Deserialize()
        {
            this.CallCount++;

            return TransitiveAspectsManifest.Create(
                ImmutableArray<InheritableAspectInstance>.Empty,
                ImmutableArray<ITransitiveAspectsManifestExtension>.Empty,
                ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>.Empty,
                ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Empty,
                false );
        }
    }

    [Fact]
    public void SameProducerAndContent_IsAHit()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();
        var manifest = CreateManifest( 1, 2, 3 );

        var first = cache.GetOrAdd( _producerA, manifest, CompileTimeProject.Empty, deserializer.Deserialize );
        var second = cache.GetOrAdd( _producerA, manifest, CompileTimeProject.Empty, deserializer.Deserialize );

        Assert.Equal( 1, deserializer.CallCount );
        Assert.Same( first, second );
    }

    /// <summary>
    /// The hit must be on content, not on the identity of the value: a manifest recreated from equal bytes is a
    /// different instance but the same content, which is the whole point of hashing it.
    /// </summary>
    [Fact]
    public void SameContentFromADistinctInstance_IsAHit()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();

        cache.GetOrAdd( _producerA, CreateManifest( 1, 2, 3 ), CompileTimeProject.Empty, deserializer.Deserialize );
        cache.GetOrAdd( _producerA, CreateManifest( 1, 2, 3 ), CompileTimeProject.Empty, deserializer.Deserialize );

        Assert.Equal( 1, deserializer.CallCount );
    }

    [Fact]
    public void ChangedContent_IsAMiss()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();

        cache.GetOrAdd( _producerA, CreateManifest( 1, 2, 3 ), CompileTimeProject.Empty, deserializer.Deserialize );
        cache.GetOrAdd( _producerA, CreateManifest( 1, 2, 4 ), CompileTimeProject.Empty, deserializer.Deserialize );

        Assert.Equal( 2, deserializer.CallCount );
    }

    /// <summary>
    /// What pairing the producer with the hash buys: two projects that emit identical manifests keep separate
    /// entries instead of aliasing onto one.
    /// </summary>
    [Fact]
    public void SameContentFromADifferentProducer_IsAMiss()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();
        var manifest = CreateManifest( 1, 2, 3 );

        cache.GetOrAdd( _producerA, manifest, CompileTimeProject.Empty, deserializer.Deserialize );
        cache.GetOrAdd( _producerB, manifest, CompileTimeProject.Empty, deserializer.Deserialize );

        Assert.Equal( 2, deserializer.CallCount );
    }

    /// <summary>
    /// Without a consuming compile-time project there is no way to tell which copy the result would be bound to, so
    /// nothing is cached rather than risk handing it to a differently bound consumer.
    /// </summary>
    [Fact]
    public void UnknownConsumerProject_IsNeverCached()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();
        var manifest = CreateManifest( 1, 2, 3 );

        cache.GetOrAdd( _producerA, manifest, null, deserializer.Deserialize );
        cache.GetOrAdd( _producerA, manifest, null, deserializer.Deserialize );

        Assert.Equal( 2, deserializer.CallCount );
    }

    [Fact]
    public void AbsentManifest_IsNeverCached()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();

        cache.GetOrAdd( _producerA, null, CompileTimeProject.Empty, deserializer.Deserialize );
        cache.GetOrAdd( _producerA, null, CompileTimeProject.Empty, deserializer.Deserialize );

        Assert.Equal( 2, deserializer.CallCount );
    }

    /// <summary>
    /// The safety property: entries deserialized against one compile-time projection of the consumer are bound to
    /// that copy, so re-projecting the consumer must drop them rather than reuse them (issue #1710).
    /// </summary>
    [Fact]
    public void ReprojectedConsumer_DropsTheCache()
    {
        using var testContext = this.CreateTestContext();
        using var domain = new CompileTimeDomain( testContext.ServiceProvider.Global );

        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();
        var manifest = CreateManifest( 1, 2, 3 );

        // A second, distinct compile-time projection of a consumer, standing in for the same project re-projected.
        var otherConsumer = CompileTimeProject.CreateEmpty(
            testContext.ServiceProvider,
            domain,
            new AssemblyIdentity( "OtherConsumer" ),
            new AssemblyIdentity( "ml!OtherConsumer_0" ) );

        cache.GetOrAdd( _producerA, manifest, CompileTimeProject.Empty, deserializer.Deserialize );
        Assert.Equal( 1, deserializer.CallCount );

        // Same producer and content, but the consumer has been re-projected.
        cache.GetOrAdd( _producerA, manifest, otherConsumer, deserializer.Deserialize );
        Assert.Equal( 2, deserializer.CallCount );

        // Going back is a miss too: the cache holds only the entries of the currently bound projection.
        cache.GetOrAdd( _producerA, manifest, CompileTimeProject.Empty, deserializer.Deserialize );
        Assert.Equal( 3, deserializer.CallCount );
    }

    /// <summary>
    /// The path-keyed overload, used for package references, caches on the same terms.
    /// </summary>
    [Fact]
    public void PathKeyedOverload_HitsAndMisses()
    {
        var cache = new TransitiveManifestDeserializationCache();
        var deserializer = new CountingDeserializer();
        var lastWrite = new DateTime( 2026, 1, 1 );

        cache.GetOrAdd( "a.dll", lastWrite, CompileTimeProject.Empty, deserializer.Deserialize );
        cache.GetOrAdd( "a.dll", lastWrite, CompileTimeProject.Empty, deserializer.Deserialize );
        Assert.Equal( 1, deserializer.CallCount );

        // A rebuilt assembly at the same path is a miss.
        cache.GetOrAdd( "a.dll", lastWrite.AddSeconds( 1 ), CompileTimeProject.Empty, deserializer.Deserialize );
        Assert.Equal( 2, deserializer.CallCount );

        // A different assembly is a miss.
        cache.GetOrAdd( "b.dll", lastWrite, CompileTimeProject.Empty, deserializer.Deserialize );
        Assert.Equal( 3, deserializer.CallCount );
    }
}
