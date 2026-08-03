// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

#pragma warning disable VSTHRD200 // Async method names must have an "Async" suffix.

/// <summary>
/// Tests the retention behaviour of <see cref="ProjectVersionProvider"/> in isolation from the rest of the
/// design-time pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProjectVersionProvider"/> is the component that keeps the history of the versions of a project, so it
/// is the first place where a version can be retained for longer than intended. Its entire state is designed to be
/// held weakly: the graph of differences is stored in a <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/>
/// keyed by <see cref="Compilation"/>, and the last known compilation of each project is stored in a
/// <see cref="WeakReference{T}"/>. The tests below assert that property directly, which is the strongest statement
/// that can be made about this component: once the caller has released a compilation, the provider must not keep it
/// alive.
/// </para>
/// <para>
/// These tests complement <see cref="DesignTimePipelineMemoryLeakTests"/>. A failure here localises the defect in the
/// diff subsystem, whereas a failure there with no failure here localises it elsewhere in the pipeline.
/// </para>
/// </remarks>
public sealed class ProjectVersionProviderMemoryLeakTests : DesignTimeTestBase
{
    public ProjectVersionProviderMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Returns the source of the run-time file for a given version.
    /// </summary>
    private static string GetCode( int version )
        => $$"""
             public class C
             {
                 public int M() => {{version}};
             }
             """;

    /// <summary>
    /// Verifies that a chain of successive differences, each computed from the immediately preceding version, retains
    /// none of the versions once the caller has released them.
    /// </summary>
    /// <remarks>
    /// This is the sequence produced by an uninterrupted editing session in which the pipeline keeps up with the user.
    /// </remarks>
    [Fact]
    public async Task SequentialDiffChain_NoCompilationIsRetained()
    {
        using var testContext = this.CreateTestContext();
        var provider = new ProjectVersionProvider( testContext.ServiceProvider, true );

        var compilations = await RunSequentialChainAsync( testContext, provider, nameof(this.SequentialDiffChain_NoCompilationIsRetained), 30 );

        MemoryLeakAssert.AtMostAlive( compilations, 0, "compilations of a sequential difference chain", ("projectVersionProvider", provider) );
    }

    /// <summary>
    /// Computes a chain of differences and returns weak references to every version, including the last one.
    /// </summary>
    /// <remarks>
    /// Every strong reference to a compilation is confined to this method, so that none of them survives in the frame
    /// of the calling test method.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference[]> RunSequentialChainAsync(
        TestContext testContext,
        ProjectVersionProvider provider,
        string assemblyName,
        int versionCount )
    {
        var current = CreateInitialCompilation( testContext, assemblyName );
        _ = await provider.GetCompilationChangesAsync( null, current );

        var weakReferences = new WeakReference[versionCount];

        for ( var version = 0; version < versionCount; version++ )
        {
            var next = ReplaceCode( current, "Code.cs", version + 1 );
            _ = await provider.GetCompilationChangesAsync( current, next );
            weakReferences[version] = new WeakReference( current );
            current = next;
        }

        return weakReferences;
    }

    /// <summary>
    /// Verifies that repeatedly computing the difference from one pinned version to each successive version does not
    /// retain the intermediate versions.
    /// </summary>
    /// <remarks>
    /// This is the sequence produced when the pipeline is paused, which happens as soon as the user edits compile-time
    /// code and lasts until the next external build. Every compilation produced during that period is compared with
    /// the last version that the pipeline analysed, and the provider merges the difference it already knows with the
    /// difference from its most recent version. That merge is the code path most likely to build a chain of versions,
    /// and a user who edits an aspect and then continues to work on run-time code stays on it for a long time.
    /// </remarks>
    [Fact]
    public async Task DiffsFromPinnedVersion_IntermediateCompilationsAreNotRetained()
    {
        using var testContext = this.CreateTestContext();
        var provider = new ProjectVersionProvider( testContext.ServiceProvider, true );

        var pinned = CreateInitialCompilation( testContext, nameof(this.DiffsFromPinnedVersion_IntermediateCompilationsAreNotRetained) );
        _ = await provider.GetCompilationChangesAsync( null, pinned );

        var intermediateCompilations = await RunPinnedChainAsync( provider, pinned, 30 );

        MemoryLeakAssert.AtMostAlive(
            intermediateCompilations,
            1,
            "intermediate compilations compared against a pinned version",
            ("projectVersionProvider", provider) );

        // The pinned compilation is deliberately kept alive until the end of the test, because it plays the role of
        // the version that the paused pipeline still serves results for.
        GC.KeepAlive( pinned );
    }

    /// <summary>
    /// Computes the difference from a single pinned version to each of a series of successive versions.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference[]> RunPinnedChainAsync( ProjectVersionProvider provider, Compilation pinned, int versionCount )
    {
        var weakReferences = new WeakReference[versionCount];

        for ( var version = 0; version < versionCount; version++ )
        {
            var next = ReplaceCode( pinned, "Code.cs", version + 1 );
            _ = await provider.GetCompilationChangesAsync( pinned, next );
            weakReferences[version] = new WeakReference( next );
        }

        return weakReferences;
    }

    /// <summary>
    /// Verifies that the versions of a referenced project are not retained when the referencing project is
    /// re-analysed.
    /// </summary>
    /// <remarks>
    /// A solution is a graph of projects, and a version of a project holds a version of each project it references.
    /// Editing a project that is low in the reference graph therefore produces a new version of every project above
    /// it. If any of those versions is retained, the amount of memory involved is a multiple of the size of the
    /// solution rather than of the size of a single project, which matches the order of magnitude in the reports.
    /// </remarks>
    [Fact]
    public async Task ReferencedProjectVersions_AreNotRetained()
    {
        using var testContext = this.CreateTestContext();
        var provider = new ProjectVersionProvider( testContext.ServiceProvider, true );

        var compilations = await RunReferenceChainAsync( testContext, provider, nameof(this.ReferencedProjectVersions_AreNotRetained), 15 );

        MemoryLeakAssert.AtMostAlive(
            compilations,
            0,
            "compilations of a referenced project and of its dependent project",
            ("projectVersionProvider", provider) );
    }

    /// <summary>
    /// Edits a referenced project repeatedly and computes, for every version, the difference of the dependent project.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference[]> RunReferenceChainAsync(
        TestContext testContext,
        ProjectVersionProvider provider,
        string assemblyName,
        int versionCount )
    {
        var masterCode = new Dictionary<string, string> { ["Master.cs"] = GetCode( 0 ) };
        Compilation currentMaster = testContext.CreateCSharpCompilation( masterCode, assemblyName: assemblyName + ".Master" );

        var dependentCode = new Dictionary<string, string> { ["Dependent.cs"] = "public class D { }" };

        Compilation currentDependent = testContext.CreateCSharpCompilation(
            dependentCode,
            assemblyName: assemblyName + ".Dependent",
            additionalReferences: new[] { currentMaster.ToMetadataReference() } );

        _ = await provider.GetCompilationChangesAsync( null, currentDependent );

        // Two weak references per iteration: one for the version of the referenced project and one for the version of
        // the dependent project.
        var weakReferences = new WeakReference[versionCount * 2];

        for ( var version = 0; version < versionCount; version++ )
        {
            var nextMaster = ReplaceCode( currentMaster, "Master.cs", version + 1 );

            var oldReference = currentDependent.References
                .OfType<CompilationReference>()
                .Single( r => ReferenceEquals( r.Compilation, currentMaster ) );

            var nextDependent = currentDependent.ReplaceReference( oldReference, nextMaster.ToMetadataReference() );

            _ = await provider.GetCompilationChangesAsync( currentDependent, nextDependent );

            weakReferences[( version * 2 ) + 0] = new WeakReference( currentMaster );
            weakReferences[( version * 2 ) + 1] = new WeakReference( currentDependent );

            currentMaster = nextMaster;
            currentDependent = nextDependent;
        }

        return weakReferences;
    }

    /// <summary>
    /// Creates the first version of the simulated project.
    /// </summary>
    private static Compilation CreateInitialCompilation( TestContext testContext, string assemblyName )
        => testContext.CreateCSharpCompilation( new Dictionary<string, string> { ["Code.cs"] = GetCode( 0 ) }, assemblyName: assemblyName );

    /// <summary>
    /// Produces the next version of a compilation by replacing the syntax tree of one source file, in the same way as
    /// Roslyn does when the user types.
    /// </summary>
    private static Compilation ReplaceCode( Compilation compilation, string fileName, int version )
    {
        var oldTree = compilation.SyntaxTrees.Single( t => string.Equals( t.FilePath, fileName, StringComparison.OrdinalIgnoreCase ) );
        var newTree = CSharpSyntaxTree.ParseText( GetCode( version ), (CSharpParseOptions) oldTree.Options, fileName, oldTree.Encoding );

        return compilation.ReplaceSyntaxTree( oldTree, newTree );
    }
}
