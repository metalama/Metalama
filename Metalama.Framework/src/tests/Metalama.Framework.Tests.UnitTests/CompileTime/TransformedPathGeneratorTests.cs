// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the uniqueness contract of <see cref="CompileTimeCompilationBuilder.TransformedPathGenerator"/>, one of the
/// sites of issue https://github.com/metalama/Metalama/issues/1742.
/// </summary>
/// <remarks>
/// <para>
/// This site is the odd one of the family. It does not key on the file path at all: it keys on the file name without
/// its directory, combined with the <em>low thirty-two bits</em> of the content hash. Two consequences follow. It is
/// reachable at compile time, unlike the other sites, because the command-line compiler deduplicates source paths but
/// says nothing about file names. And its message, which asserts that the two files have "exactly the same code", is
/// wrong in the case that reaches production: two files of the same name whose contents differ but whose truncated
/// hashes collide.
/// </para>
/// <para>
/// The transformed name must remain independent of the directory in which the repository is checked out, so that one
/// project produces one source hash on every machine, and it is bounded by
/// <see cref="OutputPathHelper.MaxOutputFilenameLength"/>. A resolution therefore has to be deterministic and short.
/// The caller orders its input by name and then by hash before calling, so a collision ordinal derived from the call
/// order satisfies both.
/// </para>
/// </remarks>
public sealed class TransformedPathGeneratorTests
{
    /// <summary>
    /// Two compile-time files of the same name whose contents differ, but whose hashes agree in their low thirty-two
    /// bits. The generator sees one collision and throws, claiming the two files hold identical code.
    /// </summary>
    [Fact]
    public void TruncatedHashCollisionOnSameFileName()
    {
        var generator = new CompileTimeCompilationBuilder.TransformedPathGenerator();

        // Distinct 64-bit hashes, hence distinct file contents, agreeing on the 32 bits that reach the name.
        const ulong firstHash = 0x1111_1111_ABCD_EF01;
        const ulong secondHash = 0x2222_2222_ABCD_EF01;

        var firstPath = generator.GetTransformedFilePath( "Fabric", firstHash );
        var secondPath = generator.GetTransformedFilePath( "Fabric", secondHash );

        Assert.NotEqual( firstPath, secondPath );
    }

    /// <summary>
    /// Two compile-time files of the same name and identical content, the case the current message describes. It is
    /// the rarer of the two, because identical content in two files usually means duplicate type declarations, but the
    /// generator must not fail the whole project over it either.
    /// </summary>
    [Fact]
    public void SameFileNameAndSameContent()
    {
        var generator = new CompileTimeCompilationBuilder.TransformedPathGenerator();

        const ulong hash = 0x0123_4567_89AB_CDEF;

        var firstPath = generator.GetTransformedFilePath( "Fabric", hash );
        var secondPath = generator.GetTransformedFilePath( "Fabric", hash );

        Assert.NotEqual( firstPath, secondPath );
    }

    /// <summary>
    /// The resolution of a collision must be a function of the call order alone, because the caller derives that order
    /// from the file name and the content hash and never from the directory. Two generators fed the same sequence must
    /// therefore produce the same names, on any machine and from any checkout directory.
    /// </summary>
    [Fact]
    public void CollisionResolutionIsDeterministic()
    {
        static IReadOnlyList<string> Generate()
        {
            var generator = new CompileTimeCompilationBuilder.TransformedPathGenerator();

            return new[]
            {
                generator.GetTransformedFilePath( "Fabric", 0x1111_1111_ABCD_EF01 ),
                generator.GetTransformedFilePath( "Fabric", 0x2222_2222_ABCD_EF01 ),
                generator.GetTransformedFilePath( "Fabric", 0x3333_3333_ABCD_EF01 )
            };
        }

        Assert.Equal( Generate(), Generate() );
    }

    /// <summary>
    /// The control case: names that do not collide must keep the shape they have today, because the transformed path
    /// participates in the compile-time project hash and any change to it invalidates every cached compile-time
    /// project.
    /// </summary>
    [Fact]
    public void DistinctNamesAreUnaffected()
    {
        var generator = new CompileTimeCompilationBuilder.TransformedPathGenerator();

        Assert.Equal( "Aspect_abcdef01.cs", generator.GetTransformedFilePath( "Aspect", 0x1111_1111_ABCD_EF01 ) );
        Assert.Equal( "Fabric_abcdef01.cs", generator.GetTransformedFilePath( "Fabric", 0x1111_1111_ABCD_EF01 ) );
    }

    /// <summary>
    /// Documents that the throw, and not merely the collision, is the defect: the generator is asked for a path and
    /// fails the entire compile-time project instead of returning one.
    /// </summary>
    [Fact]
    public void CollisionDoesNotThrow()
    {
        var generator = new CompileTimeCompilationBuilder.TransformedPathGenerator();

        _ = generator.GetTransformedFilePath( "Fabric", 0x1111_1111_ABCD_EF01 );

        var exception = Record.Exception( () => generator.GetTransformedFilePath( "Fabric", 0x2222_2222_ABCD_EF01 ) );

        Assert.Null( exception );
    }
}
