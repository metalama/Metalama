// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

public sealed class RecursiveFileSystemWatcherTests : UnitTestClass
{
    [Fact]
    public async Task ExistingDirectory()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always, Guid.NewGuid().ToString() );

        using var watcher = new RecursiveFileSystemWatcher( tempDirectory, "file.txt" );

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

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt" );

        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        Directory.CreateDirectory( directory );

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 1 ) ) ) );
    }

    [Fact]
    public async Task MissingGrandparent()
    {
        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always );

        var directory = Path.Combine( tempDirectory, "Parent", "Child" );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt" );

        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        Directory.CreateDirectory( directory );

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 1 ) ) ) );
    }

    [Fact]
    public async Task MissingParentWithRace()
    {
        // Tests that creating the directory after the watcher is created, but before it's enabled, still raises the event.

        using var testContext = this.CreateTestContext();
        var tempFileManager = testContext.ServiceProvider.Underlying.GetRequiredBackstageService<ITempFileManager>();
        var tempDirectory = tempFileManager.GetTempDirectory( "RecursiveFileSystemWatcherTests", CleanUpStrategy.Always );

        var directory = Path.Combine( tempDirectory, "Child" );

        using var watcher = new RecursiveFileSystemWatcher( directory, "file.txt" );

        Directory.CreateDirectory( directory );
        var wasRaised = new TaskCompletionSource<bool>();
        watcher.Changed += ( _, _ ) => wasRaised.TrySetResult( true );
        watcher.EnableRaisingEvents = true;

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText( Path.Combine( directory, "file.txt" ), "test" );

        Assert.Same( wasRaised.Task, await Task.WhenAny( wasRaised.Task, Task.Delay( TimeSpan.FromSeconds( 1 ) ) ) );
    }
}