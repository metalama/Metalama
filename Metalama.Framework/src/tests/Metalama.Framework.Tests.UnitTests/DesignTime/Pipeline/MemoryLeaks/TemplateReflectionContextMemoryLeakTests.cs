// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that the cacheable template reflection context, and the provider that creates it, do not retain the
/// compilation they were derived from.
/// </summary>
/// <remarks>
/// <para>
/// A template declared in a referenced assembly cannot be discovered against the compilation that Roslyn supplies,
/// because that compilation imports only the public and protected members of its references, and a template is
/// frequently neither. <see cref="CacheableTemplateDiscoveryContextProvider"/> therefore creates a compilation of its
/// own, with <c>MetadataImportOptions.All</c> and no syntax tree, and template discovery runs against that one.
/// </para>
/// <para>
/// That synthetic compilation is what the word "cacheable" refers to: it holds only
/// <see cref="PortableExecutableReference"/> instances, whose symbols belong to a reference manager that Roslyn shares
/// between compilations, so it is independent of any version of the project and may be kept for a whole design-time
/// session. The source compilation is a different matter. It is an input of the construction and nothing more, yet the
/// provider stores it, and both the provider and the context it creates are reachable from
/// <c>AspectPipelineConfiguration</c>, which is reused across keystrokes. See issue #1808.
/// </para>
/// </remarks>
public sealed class TemplateReflectionContextMemoryLeakTests : UnitTestClass
{
    public TemplateReflectionContextMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _code = """
                                 public class C
                                 {
                                     public int Method() => 42;
                                 }
                                 """;

    /// <summary>
    /// Verifies that the provider releases the source compilation once it has created the context.
    /// </summary>
    /// <remarks>
    /// This is the path that <c>CompileTimeProject</c> follows: it holds the provider rather than the context, because
    /// <c>CompileTimeProject.TemplateReflectionContext</c> resolves through it on every call, and a
    /// <c>CompileTimeProject</c> is reachable from the pipeline configuration both directly and through the
    /// compile-time project repository.
    /// </remarks>
    [Fact]
    public void Provider_DoesNotRetainTheSourceCompilation()
    {
        using var testContext = this.CreateTestContext();

        var provider = CreateProvider( testContext, out var sourceCompilation );

        // Forcing the creation of the context is what the pipeline does, and it is the point after which the source
        // compilation is no longer needed for anything.
        Assert.NotNull( provider.GetTemplateDiscoveryContext() );

        MemoryLeakAssert.Collected(
            sourceCompilation,
            "The compilation from which the cacheable template reflection context was derived",
            ("provider", provider) );
    }

    /// <summary>
    /// Verifies that the context itself releases the source compilation.
    /// </summary>
    /// <remarks>
    /// This is the path that <c>TemplateClass</c> follows. <c>TemplateClass</c> stores the context only when
    /// <see cref="ITemplateReflectionContext.IsCacheable"/> is <c>true</c>, precisely so that the pipeline
    /// configuration does not retain a compilation, and that precaution is defeated if the cacheable context reaches
    /// the source compilation through the provider that created it.
    /// </remarks>
    [Fact]
    public void CacheableContext_DoesNotRetainTheSourceCompilation()
    {
        using var testContext = this.CreateTestContext();

        var context = CreateContext( testContext, out var sourceCompilation );

        Assert.True( context.IsCacheable );

        MemoryLeakAssert.Collected(
            sourceCompilation,
            "The compilation from which the cacheable template reflection context was derived",
            ("context", context) );
    }

    /// <summary>
    /// Verifies that the compilation model that the context builds for template reflection does not reach the source
    /// compilation either.
    /// </summary>
    /// <remarks>
    /// The model is built lazily, so a test that never asks for it leaves a closure unevaluated and does not state
    /// anything about what that closure captures. The pipeline evaluates it as soon as an aspect declares advice, and
    /// the model then survives for as long as the context does.
    /// </remarks>
    [Fact]
    public void CacheableContextWithCompilationModel_DoesNotRetainTheSourceCompilation()
    {
        using var testContext = this.CreateTestContext();

        var context = CreateContext( testContext, out var sourceCompilation );

        // The cacheable implementation ignores this argument and reflects templates against its own compilation, so
        // passing no source compilation keeps the test free of any compilation other than the one under examination.
        var compilationModel = context.GetCompilationModel( null! );

        Assert.NotNull( compilationModel );

        MemoryLeakAssert.Collected(
            sourceCompilation,
            "The compilation from which the cacheable template reflection context was derived",
            ("context", context),
            ("compilationModel", compilationModel) );
    }

    /// <summary>
    /// Verifies that the synthetic compilation is the one against which templates are reflected, and that it carries
    /// the metadata import options without which the non-public templates of a referenced assembly are invisible.
    /// </summary>
    /// <remarks>
    /// The other tests of this class state what the context must not retain. This one states what it must be, so that
    /// a change that satisfied them by not creating the synthetic compilation at all would still fail.
    /// </remarks>
    [Fact]
    public void CacheableContext_UsesASyntheticCompilationThatImportsAllMetadata()
    {
        using var testContext = this.CreateTestContext();

        var context = CreateContext( testContext, out _ );

        Assert.Empty( context.Compilation.SyntaxTrees );
        Assert.All( context.Compilation.References, reference => Assert.IsAssignableFrom<PortableExecutableReference>( reference ) );
        Assert.NotEmpty( context.Compilation.References );
        Assert.Equal( MetadataImportOptions.All, context.Compilation.Options.MetadataImportOptions );
    }

    /// <summary>
    /// Creates a provider over a fresh compilation and returns a weak reference to that compilation.
    /// </summary>
    /// <remarks>
    /// The compilation is never returned by a strong reference, and the method is not inlinable, so that the local
    /// variable that holds it belongs to a stack frame that no longer exists when the caller resumes. A debug build
    /// keeps every local alive until the end of the method that declares it, therefore a test that created the
    /// compilation in its own body would retain it whatever the product code does.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static CacheableTemplateDiscoveryContextProvider CreateProvider( TestContext testContext, out WeakReference sourceCompilation )
    {
        var compilation = testContext.CreateCSharpCompilation( _code );

        var provider = new CacheableTemplateDiscoveryContextProvider( compilation, testContext.ServiceProvider );

        // Without this call the provider concludes that no reference contains compile-time code and returns no
        // context at all, because the source compilation is then sufficient for template discovery.
        provider.OnPortableExecutableReferenceDiscovered();

        sourceCompilation = new WeakReference( compilation );

        return provider;
    }

    /// <summary>
    /// Creates a cacheable context over a fresh compilation and returns a weak reference to that compilation, without
    /// retaining the provider.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static ITemplateReflectionContext CreateContext( TestContext testContext, out WeakReference sourceCompilation )
        => CreateProvider( testContext, out sourceCompilation ).GetTemplateDiscoveryContext()!;
}
