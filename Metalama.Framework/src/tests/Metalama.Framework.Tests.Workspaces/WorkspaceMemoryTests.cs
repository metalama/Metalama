// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Workspaces;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200

namespace Metalama.Framework.Tests.Workspaces;

/// <summary>
/// Tests that a <see cref="Workspace"/> releases the compilations of a load when it is reloaded or disposed.
/// </summary>
/// <remarks>
/// A long-running host, such as the Metalama MCP server, reloads a workspace after every edit and disposes it on
/// request. Each load holds the Roslyn compilations, the code models and the reference index of every project, so a
/// previous load that stays reachable keeps hundreds of megabytes alive.
/// </remarks>
public sealed class WorkspaceMemoryTests : UnitTestClass
{
    /// <summary>
    /// The maximum number of times that <see cref="AssertCollectedAsync"/> leaves the current stack and collects before
    /// it fails.
    /// </summary>
    private const int _maxCollectionAttempts = 20;

    private const string _code = """
                                 class A;
                                 class B : A
                                 {
                                   A f;
                                 }
                                 partial class C
                                 {
                                   [System.Text.RegularExpressions.GeneratedRegex( "a+" )]
                                   public static partial System.Text.RegularExpressions.Regex Pattern();
                                 }
                                 """;

    [Fact]
    public async Task ReloadReleasesThePreviousCompilations()
    {
        using var testContext = this.CreateTestContext();
        var projectPath = await CreateProjectAsync( testContext );
        var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

        using var workspace = await workspaceCollection.LoadAsync(
            [projectPath],
            WorkspaceTests.DefaultProperties,
            cancellationToken: testContext.CancellationToken );

        WorkspaceTests.CheckWorkspace( workspace );

        var previous = QueryInboundReferences( workspace );

        await workspace.ReloadAsync( restore: false, testContext.CancellationToken );
        WorkspaceTests.CheckWorkspace( workspace );
        QueryInboundReferences( workspace );

        await AssertCollectedAsync( previous.CompilationModel, "the code model of the previous load" );
        await AssertCollectedAsync( previous.RoslynCompilation, "the Roslyn compilation of the previous load" );
    }

    [Fact]
    public async Task DisposeReleasesTheCompilations()
    {
        using var testContext = this.CreateTestContext();
        var projectPath = await CreateProjectAsync( testContext );
        var workspaceCollection = new WorkspaceCollection( testContext.ServiceProvider );

        var workspace = await workspaceCollection.LoadAsync(
            [projectPath],
            WorkspaceTests.DefaultProperties,
            cancellationToken: testContext.CancellationToken );

        WorkspaceTests.CheckWorkspace( workspace );

        var references = QueryInboundReferences( workspace );
        var workspaceReference = new WeakReference( workspace );

        workspace.Dispose();
        workspace = null;

        await AssertCollectedAsync( workspaceReference, "the disposed workspace" );
        await AssertCollectedAsync( references.CompilationModel, "the code model of the disposed workspace" );
        await AssertCollectedAsync( references.RoslynCompilation, "the Roslyn compilation of the disposed workspace" );

        // The collection still holds no reference to the disposed workspace, and loads it again from disk.
        using var reloaded = await workspaceCollection.LoadAsync(
            [projectPath],
            WorkspaceTests.DefaultProperties,
            cancellationToken: testContext.CancellationToken );

        Assert.False( workspaceReference.IsAlive );
        WorkspaceTests.CheckWorkspace( reloaded );
    }

    /// <summary>
    /// Creates a project that references the Metalama assemblies and uses the regular expression source generator of the
    /// .NET SDK.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A workspace that removed every source generator, and not only the Metalama one, would fail
    /// <see cref="WorkspaceTests.CheckWorkspace"/>, because the partial method that the regular expression source generator
    /// implements would have no implementation.
    /// </para>
    /// <para>
    /// The project does not reference the Metalama source generator: without the build properties of the Metalama package,
    /// the generator considers Metalama disabled and does nothing. <c>Metalama.Mcp.Tests</c> loads a project that references
    /// the Metalama package, and <see cref="SourceGeneratorFilterTests"/> tests the removal of the analyzer reference.
    /// </para>
    /// </remarks>
    private static Task<string> CreateProjectAsync( MetalamaTestContext testContext ) => WorkspaceTests.CreateMetalamaEnabledProjectAsync( testContext, _code );

    /// <summary>
    /// Builds the reference index of the only project of a workspace, which is its largest structure, and returns weak
    /// references to the code model and to the Roslyn compilation of the project.
    /// </summary>
    /// <remarks>
    /// The method is not inlined, so that no local variable of the caller references the compilation.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static (WeakReference CompilationModel, WeakReference RoslynCompilation) QueryInboundReferences( Workspace workspace )
    {
        var project = workspace.Projects.Single();
        var typeA = project.Types.Single( t => t.Name == "A" );

        var references = typeA.GetInboundReferences().Select( r => r.OriginDeclaration.ToDisplayString() ).OrderBy( r => r ).ToList();

        Assert.Equal( ["B", "B.f"], references );

        // The source generators other than the Metalama one still run.
        Assert.Contains( project.RoslynCompilation.SyntaxTrees, t => t.FilePath.Contains( "RegexGenerator", StringComparison.Ordinal ) );

        return (new WeakReference( typeA.Compilation ), new WeakReference( project.RoslynCompilation ));
    }

    /// <summary>
    /// Asserts that the target of a weak reference has been collected, after resuming the calling asynchronous method on
    /// a thread of the thread pool.
    /// </summary>
    /// <remarks>
    /// Without a synchronization context, the continuation of an <c>await</c> runs on the thread that completed the
    /// awaited task, above the stack frames of the code under test, and the local variables of these frames can still
    /// reference the objects that the test expects to be collected. <see cref="Task.Yield"/> lets that thread return from
    /// these frames. No API lets the test wait for this, so the method yields and collects up to
    /// <see cref="_maxCollectionAttempts"/> times. None of these attempts waits for a fixed time.
    /// </remarks>
    private static async Task AssertCollectedAsync( WeakReference weakReference, string description )
    {
        for ( var attempt = 0; attempt < _maxCollectionAttempts; attempt++ )
        {
            await Task.Yield();

            if ( !IsAlive( weakReference ) )
            {
                return;
            }
        }

        Assert.Fail( $"Expected {description} to be collected, but it is still reachable." );
    }

    /// <summary>
    /// Performs several rounds of blocking, compacting garbage collection and returns whether the target of a weak
    /// reference is still alive.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static bool IsAlive( WeakReference weakReference )
    {
        for ( var i = 0; i < 4; i++ )
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );
            GC.WaitForPendingFinalizers();
        }

        GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );

        return weakReference.IsAlive;
    }
}
