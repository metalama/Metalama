// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Options;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests the cache through which an identifier-based durable reference remembers the reference it last resolved to.
/// </summary>
/// <remarks>
/// The cache exists because resolving an identifier walks the symbol table, which is a large constant on an operation
/// that is not rare. It is required to leave the result of every resolution unchanged, and that requirement is what
/// <see cref="SerializableRefTests"/> and <see cref="UncachedSerializableRefTests"/> hold, by running the same assertions
/// with the cache enabled and disabled. What is left to test here is that the cache is populated, that it is honoured,
/// and that it is bypassed when it must be. See issue #1811.
/// </remarks>
public sealed class DurableRefResolutionCacheTests : UnitTestClass
{
    private const string _code = "class C { }";

    private static DurableRef<INamedType> GetDurableRef( CompilationModel compilation )
        => (DurableRef<INamedType>) compilation.Types.OfName( "C" ).Single().ToRef().ToDurable();

    /// <summary>
    /// Verifies that a resolution populates the cache, and that a second resolution against the same compilation
    /// produces the same declaration.
    /// </summary>
    [Fact]
    public void ResolutionPopulatesTheCache()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serializable } );
        var compilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.False( durableRef.IsResolutionCached );

        var first = durableRef.GetTarget( compilation );

        Assert.True( durableRef.IsResolutionCached );

        Assert.Same( first, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that the cache is neither populated nor consulted when the project asks for it to be disabled.
    /// </summary>
    [Fact]
    public void ResolutionDoesNotPopulateTheCacheWhenItIsDisabled()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.SerializableWithoutCache } );
        var compilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );

        Assert.False( durableRef.IsResolutionCached );
    }

    /// <summary>
    /// Verifies that a reference that holds the compilation it was made from never populates the cache, because it has
    /// nothing to gain from it.
    /// </summary>
    [Fact]
    public void ALiveRefDoesNotUseTheCache()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Live } );
        var compilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );

        Assert.False( durableRef.IsResolutionCached );
    }

    /// <summary>
    /// Verifies that a cached reference is not reused against an unrelated compilation.
    /// </summary>
    /// <remarks>
    /// The cached reference belongs to one <c>RefFactory</c>, which one lineage of compilation models shares and no
    /// other compilation has. Resolving against a compilation of another lineage must therefore go through the
    /// identifier, and must answer with the declaration of that compilation rather than with the cached one.
    /// </remarks>
    [Fact]
    public void ACachedRefIsNotReusedAcrossCompilations()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serializable } );

        var compilation = testContext.CreateCompilationModel( _code );
        var otherCompilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );
        Assert.Same( otherCompilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( otherCompilation ) );
    }

    /// <summary>
    /// Verifies that a cached reference is reused across the versions of one compilation, which is where the cache
    /// earns its place: the pipeline produces a new compilation model at every step, and they share a <c>RefFactory</c>.
    /// </summary>
    [Fact]
    public void ACachedRefIsReusedAcrossTheVersionsOfOneCompilation()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serializable } );

        var compilation = testContext.CreateCompilationModel( _code );
        var derivedCompilation = compilation.CreateMutableClone();

        var durableRef = GetDurableRef( compilation );

        _ = durableRef.GetTarget( compilation );
        Assert.True( durableRef.IsResolutionCached );

        Assert.Same( derivedCompilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( derivedCompilation ) );
        Assert.True( durableRef.IsResolutionCached );
    }
}
