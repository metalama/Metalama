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
/// Tests the cache in which an identifier-based durable reference stores the reference returned by its last
/// resolution.
/// </summary>
/// <remarks>
/// The cache exists because resolving an identifier requires a lookup in the symbol table, and references are resolved
/// frequently. The cache must return the same result as a resolution that does not use it.
/// <see cref="SerializedRefTests"/> and <see cref="UncachedSerializedRefTests"/> verify that requirement, by
/// running the same assertions with the cache enabled and disabled. The tests in this class verify that the cache is
/// populated, that it is used, and that it is not used when it must not be. See issue #1811.
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
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serialized } );
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
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.SerializedWithoutCache } );
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
    public void ABoundRefDoesNotUseTheCache()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Bound } );
        var compilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );

        Assert.False( durableRef.IsResolutionCached );
    }

    /// <summary>
    /// Verifies that a cached reference is not reused against an unrelated compilation.
    /// </summary>
    /// <remarks>
    /// The cached reference belongs to a single <c>RefFactory</c>, which is shared by all versions of one compilation
    /// model and by no other compilation. A resolution in another compilation must therefore use the identifier, and
    /// must return the declaration of that compilation and not the cached one.
    /// </remarks>
    [Fact]
    public void ACachedRefIsNotReusedAcrossCompilations()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serialized } );

        var compilation = testContext.CreateCompilationModel( _code );
        var otherCompilation = testContext.CreateCompilationModel( _code );

        var durableRef = GetDurableRef( compilation );

        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );
        Assert.Same( otherCompilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( otherCompilation ) );
    }

    /// <summary>
    /// Verifies that a cached reference is reused across the versions of a single compilation model. This is the case
    /// in which the cache is effective, because the pipeline creates a new compilation model at each step, and all
    /// these versions share a <c>RefFactory</c>.
    /// </summary>
    [Fact]
    public void ACachedRefIsReusedAcrossTheVersionsOfOneCompilation()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = DurableRefKind.Serialized } );

        var compilation = testContext.CreateCompilationModel( _code );
        var derivedCompilation = compilation.CreateMutableClone();

        var durableRef = GetDurableRef( compilation );

        _ = durableRef.GetTarget( compilation );
        Assert.True( durableRef.IsResolutionCached );

        Assert.Same( derivedCompilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( derivedCompilation ) );
        Assert.True( durableRef.IsResolutionCached );
    }
}
