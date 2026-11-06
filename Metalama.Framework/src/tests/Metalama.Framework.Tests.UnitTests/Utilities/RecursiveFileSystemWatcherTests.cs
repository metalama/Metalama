// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

public sealed class RecursiveFileSystemWatcherTests : UnitTestClass
{
    /// <summary>
    /// The reporter given to the watchers of the tests that do not expect any exception. Their handlers do not throw, and
    /// a failure of the watcher itself shows as an event that the test awaits and never receives.
    /// </summary>
    private static void OnWatcherException( Exception _ ) { }

    /// <summary>
    /// An exception thrown by an event handler runs on a thread pool thread, where it would terminate the process. It must
    /// be passed to the reporter instead. See https://github.com/metalama/Metalama/issues/2317.
    /// </summary>
    [Fact]
    public async Task ExceptionInHandlerIsReported()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always, Guid.NewGuid().ToString() );

        var reportedException = new TaskCompletionSource<Exception>( TaskCreationOptions.RunContinuationsAsynchronously );

        using var watcher = new RecursiveFileSystemWatcher( tempDirectory, "file.txt", e => reportedException.TrySetResult( e ) );
        watcher.Changed += ( _, _ ) => throw new InvalidOperationException( "Thrown by the handler." );
        watcher.EnableRaisingEvents = true;

        File.WriteAllText( Path.Combine( tempDirectory, "file.txt" ), "content" );

        var exception = await reportedException.Task.WithCancellation( testContext.CancellationToken );
        Assert.IsType<InvalidOperationException>( exception );
    }

    [Fact]
    public async Task ExistingDirectory()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always, Guid.NewGuid().ToString() );

        using var watcher = new RecursiveFileSystemWatcher( tempDirectory, "file.txt", OnWatcherException );

        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( tempDirectory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 120 ) ) ) );
    }

    [Fact]
    public async Task MissingParent()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always );

        var directory = Path.Combine( tempDirectory, "Child" );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt", OnWatcherException );

        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        Directory.CreateDirectory( directory );

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 30 ) ) ) );
    }

    [Fact]
    public async Task MissingGrandparent()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always );

        var directory = Path.Combine( tempDirectory, "Parent", "Child" );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt", OnWatcherException );

        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        Directory.CreateDirectory( directory );

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 30 ) ) ) );
    }

    [Fact]
    public async Task MissingParentWithRace()
    {
        // Tests that creating the directory after the watcher is created, but before it's enabled, still raises the event.

        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always );

        var directory = Path.Combine( tempDirectory, "Child" );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt", OnWatcherException );

        Directory.CreateDirectory( directory );
        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 30 ) ) ) );
    }

    [SkippableTheory]
    [InlineData( "" )]
    [InlineData( "Child" )]
    [InlineData( "Parent\\Child" )]
    public void MissingRoot( string relativePath )
    {
        // Tests that watching a path on a drive whose root does not exist does not throw (#2046).

        var missingRoot = GetMissingDriveRoot();

        Skip.If( missingRoot == null, "No unused drive letter is available on this machine." );

        var directory = Path.Combine( missingRoot!, relativePath );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt", OnWatcherException );

        watcher.Changed += ( _, _ ) => { };
        watcher.EnableRaisingEvents = true;
        watcher.EnableRaisingEvents = false;
    }

    private static string? GetMissingDriveRoot()
    {
        if ( Path.DirectorySeparatorChar != '\\' )
        {
            // On Unix, the only root is '/', which always exists.
            return null;
        }

        for ( var letter = 'Z'; letter >= 'D'; letter-- )
        {
            var root = $"{letter}:\\";

            if ( !Directory.Exists( root ) )
            {
                return root;
            }
        }

        return null;
    }
}