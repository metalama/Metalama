// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Protocol;
using Metalama.Mcp.Worker;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Tests that the worker releases the compilations of a workspace when the workspace is reloaded or unloaded.
/// </summary>
/// <remarks>
/// The tests run a <see cref="RequestHandler"/> in the test process, because a weak reference cannot observe the memory
/// of the worker process. They load the FrogHattery sample, run the queries that build the largest structures (the
/// reference index, the result tree in every format, and the resolution of a link with an index step), and then check
/// that the compilations of the previous load are collected.
/// </remarks>
public sealed class WorkspaceMemoryTests : UnitTestClass
{
    /// <summary>
    /// The maximum number of times that <see cref="AssertCollectedAsync"/> leaves the current stack and collects before
    /// it fails.
    /// </summary>
    private const int _maxCollectionAttempts = 20;

    private const string _referencesQuery = """
                                            workspace.SourceCode.Types
                                                .Select( t => new { Type = t, References = t.GetInboundReferences().ToList() } )
                                            """;

    [Fact]
    public async Task ReloadReleasesThePreviousCompilations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new RequestHandler( CommandLineOptions.Parse( [] ) );

        await LoadAsync( handler, cancellationToken );
        await RunQueriesAsync( handler, cancellationToken );
        var previous = GetCompilations( handler );

        await CallSuccessfullyAsync( handler, WorkerMethods.Reload, new JsonObject(), cancellationToken );
        await RunQueriesAsync( handler, cancellationToken );

        await AssertCollectedAsync( previous, "the compilations of the previous load" );

        await CallSuccessfullyAsync( handler, WorkerMethods.Unload, new JsonObject(), cancellationToken );
    }

    [Fact]
    public async Task UnloadReleasesTheWorkspace()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new RequestHandler( CommandLineOptions.Parse( [] ) );

        await LoadAsync( handler, cancellationToken );
        await RunQueriesAsync( handler, cancellationToken );
        var compilations = GetCompilations( handler );
        var workspace = GetWorkspace( handler );

        await CallSuccessfullyAsync( handler, WorkerMethods.Unload, new JsonObject(), cancellationToken );

        await AssertCollectedAsync( [workspace], "the unloaded workspace" );
        await AssertCollectedAsync( compilations, "the compilations of the unloaded workspace" );
    }

    private static Task LoadAsync( RequestHandler handler, CancellationToken cancellationToken )
        => CallSuccessfullyAsync( handler, WorkerMethods.Load, new JsonObject { ["paths"] = new JsonArray( ServerProcess.FrogHatteryPath ) }, cancellationToken );

    /// <summary>
    /// Runs the queries whose results reference the code model: the reference index in each format, and the resolution of
    /// links, including a link with an index step, which carries the fingerprint of the load.
    /// </summary>
    private static async Task RunQueriesAsync( RequestHandler handler, CancellationToken cancellationToken )
    {
        foreach ( var format in new[] { "text", "json", "html" } )
        {
            await CallSuccessfullyAsync(
                handler,
                WorkerMethods.Query,
                new JsonObject { ["code"] = _referencesQuery, ["format"] = format, [WorkerMethods.IncludeStructured] = true },
                cancellationToken );
        }

        await CallSuccessfullyAsync( handler, WorkerMethods.Open, new JsonObject { ["link"] = "T:FrogHattery.Frog" }, cancellationToken );
        await CallSuccessfullyAsync( handler, WorkerMethods.Open, new JsonObject { ["link"] = "T:FrogHattery.Frog/Methods" }, cancellationToken );
    }

    private static async Task<JsonObject> CallSuccessfullyAsync( RequestHandler handler, string method, JsonObject args, CancellationToken cancellationToken )
    {
        var response = await handler.HandleAsync( new JsonObject { [WorkerMethods.Method] = method, [WorkerMethods.Args] = args }, cancellationToken );

        Assert.True( response[WorkerMethods.Ok]?.GetValue<bool>() == true, $"{method} failed: {response[WorkerMethods.Text]}" );

        return response;
    }

    /// <summary>
    /// Gets weak references to the Roslyn compilations and to the code models of the only loaded workspace.
    /// </summary>
    /// <remarks>
    /// The method is not inlined, so that no local variable of the caller references a compilation.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static List<WeakReference> GetCompilations( RequestHandler handler )
    {
        var projects = handler.Registry.Entries.Single().Workspace.Projects;

        Assert.NotEmpty( projects );

        return projects.SelectMany( p => new[] { new WeakReference( p.RoslynCompilation ), new WeakReference( p.SourceCode.Compilations[0] ) } ).ToList();
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference GetWorkspace( RequestHandler handler ) => new( handler.Registry.Entries.Single().Workspace );

    /// <summary>
    /// Asserts that the targets of weak references have been collected, after resuming the calling asynchronous method on
    /// a thread of the thread pool.
    /// </summary>
    /// <remarks>
    /// Without a synchronization context, the continuation of an <c>await</c> runs on the thread that completed the
    /// awaited task, above the stack frames of the code under test, and the local variables of these frames can still
    /// reference the objects that the test expects to be collected. <see cref="Task.Yield"/> lets that thread return from
    /// these frames. No API lets the test wait for this, so the method yields and collects up to
    /// <see cref="_maxCollectionAttempts"/> times. None of these attempts waits for a fixed time.
    /// </remarks>
    private static async Task AssertCollectedAsync( IReadOnlyList<WeakReference> weakReferences, string description )
    {
        var alive = 0;

        for ( var attempt = 0; attempt < _maxCollectionAttempts; attempt++ )
        {
            await Task.Yield();

            alive = CountAlive( weakReferences );

            if ( alive == 0 )
            {
                return;
            }
        }

        Assert.Fail( $"Expected {description} to be collected, but {alive} of {weakReferences.Count} objects are still reachable." );
    }

    /// <summary>
    /// Performs several rounds of blocking, compacting garbage collection and returns the number of weak references whose
    /// target is still alive.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static int CountAlive( IReadOnlyList<WeakReference> weakReferences )
    {
        for ( var i = 0; i < 4; i++ )
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );
            GC.WaitForPendingFinalizers();
        }

        GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );

        return weakReferences.Count( r => r.IsAlive );
    }
}
