// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Regression tests for a compile-time closure that contains two distinct projects sharing the same compile-time
/// assembly name (issue #1749).
/// </summary>
/// <remarks>
/// <para>
/// Looking a project up by compile-time assembly name used to build a <c>Dictionary</c> keyed by that name, so a
/// closure holding two projects with the same name threw <c>ArgumentException: An item with the same key has already
/// been added</c>. The reported stack traces reach that lookup from
/// <c>CompileTimeSerializationBinder.BindToName</c>, i.e. while serializing a type name, where the message says
/// nothing about the real cause.
/// </para>
/// <para>
/// Two Metalama versions in the same reference graph are the reported instance of this: both compile-time projects
/// then claim the <c>Metalama.Framework</c> compile-time assembly name.
/// </para>
/// </remarks>
public sealed class DuplicateCompileTimeAssemblyNameTests : UnitTestClass
{
    public DuplicateCompileTimeAssemblyNameTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Builds a closure in which <c>ml!Ambiguous_0</c> is claimed by two projects and <c>ml!Unique_0</c> by one.
    /// </summary>
    private static CompileTimeProject CreateAmbiguousClosure( TestContext testContext )
    {
        var first = CompileTimeProject.CreateEmpty(
            testContext.ServiceProvider,
            testContext.Domain,
            new AssemblyIdentity( "First" ),
            new AssemblyIdentity( "ml!Ambiguous_0" ) );

        var second = CompileTimeProject.CreateEmpty(
            testContext.ServiceProvider,
            testContext.Domain,
            new AssemblyIdentity( "Second" ),
            new AssemblyIdentity( "ml!Ambiguous_0" ) );

        var unique = CompileTimeProject.CreateEmpty(
            testContext.ServiceProvider,
            testContext.Domain,
            new AssemblyIdentity( "Unique" ),
            new AssemblyIdentity( "ml!Unique_0" ) );

        return CompileTimeProject.CreateEmpty(
            testContext.ServiceProvider,
            testContext.Domain,
            new AssemblyIdentity( "Root" ),
            new AssemblyIdentity( "ml!Root_0" ),
            references: [first, second, unique] );
    }

    /// <summary>
    /// An ambiguous compile-time assembly name must be reported as a failed lookup, not as an
    /// <see cref="System.ArgumentException"/> thrown from the dictionary that backs the lookup.
    /// </summary>
    [Fact]
    public void AmbiguousCompileTimeAssemblyName_IsNotFound()
    {
        using var testContext = this.CreateTestContext();

        var root = CreateAmbiguousClosure( testContext );

        Assert.False( root.TryGetProjectByCompileTimeAssemblyName( "ml!Ambiguous_0", out _ ) );
    }

    /// <summary>
    /// An ambiguity must not prevent the other projects of the same closure from being resolved.
    /// </summary>
    [Fact]
    public void UnambiguousCompileTimeAssemblyName_IsStillFound()
    {
        using var testContext = this.CreateTestContext();

        var root = CreateAmbiguousClosure( testContext );

        Assert.True( root.TryGetProjectByCompileTimeAssemblyName( "ml!Unique_0", out var unique ) );
        Assert.Equal( "Unique", unique!.RunTimeIdentity.Name );

        Assert.True( root.TryGetProjectByCompileTimeAssemblyName( "ml!Root_0", out var rootProject ) );
        Assert.Same( root, rootProject );
    }

    /// <summary>
    /// An unknown name is a failed lookup, and not confused with an ambiguous one.
    /// </summary>
    [Fact]
    public void UnknownCompileTimeAssemblyName_IsNotFound()
    {
        using var testContext = this.CreateTestContext();

        var root = CreateAmbiguousClosure( testContext );

        Assert.False( root.TryGetProjectByCompileTimeAssemblyName( "ml!Missing_0", out _ ) );
    }

    /// <summary>
    /// The map backing the lookup is derived from the immutable closure, so it must be computed once and not on
    /// every access.
    /// </summary>
    [Fact]
    public void ClosureProjectsGroupedByCompileTimeAssemblyName_IsComputedOnce()
    {
        using var testContext = this.CreateTestContext();

        var root = CreateAmbiguousClosure( testContext );

        Assert.Same( root.ClosureProjectsGroupedByCompileTimeAssemblyName, root.ClosureProjectsGroupedByCompileTimeAssemblyName );
    }

    /// <summary>
    /// The ambiguity must be reported where the closure and a diagnostic sink are both available, and the diagnostic
    /// must name the run-time assemblies of all the projects claiming the name, which is what lets a user fix their
    /// reference graph.
    /// </summary>
    [Fact]
    public void AmbiguousCompileTimeAssemblyName_IsReported()
    {
        using var testContext = this.CreateTestContext();

        var root = CreateAmbiguousClosure( testContext );

        var builder = new CompileTimeProjectRepository.Builder( testContext.Domain, testContext.ServiceProvider );
        DiagnosticBag diagnostics = new();

        builder.ReportAmbiguousCompileTimeAssemblyNames( root, diagnostics );

        var diagnostic = Assert.Single( diagnostics );
        Assert.Equal( "LAMA0077", diagnostic.Id );

        var message = diagnostic.GetMessage( CultureInfo.InvariantCulture );
        Assert.Contains( "ml!Ambiguous_0", message, StringComparison.Ordinal );
        Assert.Contains( "First", message, StringComparison.Ordinal );
        Assert.Contains( "Second", message, StringComparison.Ordinal );

        // The unambiguous projects of the same closure are not reported.
        Assert.DoesNotContain( "ml!Unique_0", message, StringComparison.Ordinal );
    }
}
