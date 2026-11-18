// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using Xunit;

#pragma warning disable CS0618 // ICompilation.ManagedResources is obsolete.

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests that <see cref="ICompilation.ManagedResources"/> returns a list and does not throw.
/// </summary>
/// <remarks>
/// Regression tests for https://github.com/metalama/Metalama/issues/2356.
/// </remarks>
public sealed class ManagedResourcesTests : UnitTestClass
{
    private const string _code = "class C { }";

    [Fact]
    public void SourceCompilationReturnsEmptyList()
    {
        using var testContext = this.CreateTestContext();

        ICompilation compilation = testContext.CreateCompilationModel( _code );

        Assert.Empty( compilation.ManagedResources );
    }

    [Fact]
    public void MutableCompilationReturnsEmptyList()
    {
        using var testContext = this.CreateTestContext();

        ICompilation compilation = testContext.CreateCompilationModel( _code ).CreateMutableClone();

        Assert.Empty( compilation.ManagedResources );
    }

    [Fact]
    public void ImmutableCloneReturnsEmptyList()
    {
        using var testContext = this.CreateTestContext();

        ICompilation compilation = testContext.CreateCompilationModel( _code ).CreateMutableClone().CreateImmutableClone();

        Assert.Empty( compilation.ManagedResources );
    }
}
