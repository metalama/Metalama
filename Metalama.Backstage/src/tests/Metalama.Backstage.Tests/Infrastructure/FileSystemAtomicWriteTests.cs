// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Testing.Hooks;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

/// <summary>
/// Tests <see cref="IFileSystem.WriteAllTextAtomically"/> against the real file system.
/// </summary>
/// <remarks>
/// <para>
/// These tests deliberately do not use <c>TestFileSystem</c>. What is being tested is the way the production
/// implementation uses the file system of the operating system, namely that a reader never observes a partially
/// written file and that the substitution is retried when a reader holds the destination. A substitute would model
/// those properties rather than exhibit them, so it could not fail when they are broken.
/// </para>
/// <para>
/// Every wait is released by another thread of the same process, through the synchronization points of
/// <see cref="ITestSynchronizationProvider"/>, so the tests are deterministic and no assertion depends on a duration.
/// </para>
/// </remarks>
public sealed class FileSystemAtomicWriteTests : IDisposable
{
    /// <summary>
    /// The content initially present in the destination, chosen so that a partially written file would differ from
    /// it in length as well as in value.
    /// </summary>
    private const string _previousContent = "previous";

    /// <summary>
    /// The content written by the tests, long enough that writing it is not a single operation of the file system,
    /// so that a reader observing a partially written file would be likely to see a truncated one.
    /// </summary>
    private static readonly string _newContent = new( 'n', 512 * 1024 );

    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung test
    /// run. It is a guard and never a synchronization mechanism: no test depends on its duration.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly ITestOutputHelper _logger;
    private readonly TestSynchronizationProvider _syncProvider;
    private readonly string _directory;
    private readonly FileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemAtomicWriteTests"/> class, and creates the directory
    /// in which the tests operate.
    /// </summary>
    /// <param name="logger">The xunit output helper.</param>
    public FileSystemAtomicWriteTests( ITestOutputHelper logger )
    {
        this._logger = logger;
        this._syncProvider = new TestSynchronizationProvider( logger.WriteLine );

        this._directory = Path.Combine( Path.GetTempPath(), "Metalama.FileSystemAtomicWriteTests", Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( this._directory );

        this._fileSystem = new FileSystem( new TestServiceProvider( this._syncProvider ) );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Releasing every point first guarantees that no thread is left pinned inside the code under test, which
        // would otherwise hold a file of the directory that is about to be deleted.
        this._syncProvider.Dispose();
        this._timeout.Dispose();

        try
        {
            Directory.Delete( this._directory, recursive: true );
        }
        catch ( Exception e )
        {
            // The clean-up of a temporary directory is not what these tests assert, so its failure is reported
            // rather than turned into a failure of whichever test happened to run last.
            this._logger.WriteLine( $"Could not delete '{this._directory}': {e.Message}" );
        }
    }

    /// <summary>
    /// The minimal service provider through which <see cref="FileSystem"/> resolves the synchronization points.
    /// </summary>
    /// <remarks>
    /// <see cref="ITestSynchronizationProvider"/> is resolved untyped, because it is shared with the layers above
    /// <c>Metalama.Backstage</c> and therefore derives from no dependency injection marker interface.
    /// </remarks>
    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly TestSynchronizationProvider _syncProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceProvider"/> class.
        /// </summary>
        /// <param name="syncProvider">The provider to return.</param>
        public TestServiceProvider( TestSynchronizationProvider syncProvider )
        {
            this._syncProvider = syncProvider;
        }

        /// <inheritdoc />
        public object? GetService( Type serviceType ) => serviceType == typeof(ITestSynchronizationProvider) ? this._syncProvider : null;
    }

    /// <summary>
    /// Gets the path of the destination of a test, which does not exist yet.
    /// </summary>
    /// <param name="name">A name unique within the test, so that a test can use several destinations.</param>
    /// <returns>The path.</returns>
    private string GetPath( string name = "file" ) => Path.Combine( this._directory, $"{name}.txt" );

    /// <summary>
    /// Gets the name of the synchronization point reached while writing <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path of the destination.</param>
    /// <returns>The name of the synchronization point.</returns>
    private static string GetSyncPointName( string path ) => FileSystem.GetSyncPointName( FileSystem.BeforeSubstitutionLocation, path );

    /// <summary>
    /// Runs an action on a thread of its own, so that a test can drive it with signals while it is blocked at a
    /// synchronization point.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>A task that completes when the action returns.</returns>
    /// <remarks>
    /// No cancellation token is passed to the scheduler on purpose: a token that is already signalled makes the
    /// delegate never run, which would leave the signals awaited by the caller unset.
    /// </remarks>
    private static Task RunOnDedicatedThreadAsync( Action action )
        => Task.Factory.StartNew( action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default );

    /// <summary>
    /// Awaits a task, failing rather than hanging if <see cref="_timeout"/> elapses first.
    /// </summary>
    /// <param name="task">The task to await.</param>
    /// <returns>A task that completes when <paramref name="task"/> does.</returns>
    private async Task WithTimeout( Task task )
    {
        var timedOut = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using ( this._timeout.Token.Register( () => timedOut.TrySetResult( true ) ) )
        {
            if ( await Task.WhenAny( task, timedOut.Task ) != task )
            {
                throw new TimeoutException( "The test timed out while waiting for the code under test." );
            }
        }

        await task;
    }

    /// <summary>
    /// Gets the names of the files that currently exist in the directory of the test.
    /// </summary>
    /// <returns>The file names, without their directory.</returns>
    private string[] GetFileNames() => Directory.GetFiles( this._directory ).Select( Path.GetFileName ).OrderBy( name => name, StringComparer.Ordinal ).ToArray()!;

    /// <summary>
    /// Verifies that the destination is created when it does not exist, and that nothing else is left behind.
    /// </summary>
    [Fact]
    public void CreatesTheFileWhenItDoesNotExist()
    {
        var path = this.GetPath();

        this._fileSystem.WriteAllTextAtomically( path, _newContent );

        Assert.Equal( _newContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that the content of an existing destination is replaced, and that nothing else is left behind.
    /// </summary>
    [Fact]
    public void ReplacesTheContentOfAnExistingFile()
    {
        var path = this.GetPath();
        File.WriteAllText( path, _previousContent );

        this._fileSystem.WriteAllTextAtomically( path, _newContent );

        Assert.Equal( _newContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that a reader observes the whole previous content for as long as the new content has not been
    /// substituted, which is the property that the callers depend on.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// The assertion is made while the writer is pinned between the two operations, which is the only moment at
    /// which a plain write would have left a truncated file on disk.
    /// </remarks>
    [Fact]
    public async Task ReaderObservesThePreviousContentUntilTheSubstitution()
    {
        var path = this.GetPath();
        File.WriteAllText( path, _previousContent );

        var syncPointName = GetSyncPointName( path );
        this._syncProvider.EnableSyncPoint( syncPointName );

        var writer = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, _newContent ) );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        // The new content is on disk in full, but not under the name of the destination.
        Assert.Equal( _previousContent, File.ReadAllText( path ) );

        var names = this.GetFileNames();
        Assert.Equal( 2, names.Length );
        var temporaryName = Assert.Single( names, name => name != Path.GetFileName( path ) );
        Assert.Equal( _newContent, File.ReadAllText( Path.Combine( this._directory, temporaryName ) ) );

        // A caller enumerating the directory by the extension of the destination does not observe the temporary file.
        Assert.Equal( new[] { Path.GetFileName( path ) }, Directory.GetFiles( this._directory, "*.txt" ).Select( Path.GetFileName ).ToArray() );

        this._syncProvider.DisableSyncPoint( syncPointName );
        await this.WithTimeout( writer );

        Assert.Equal( _newContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that a destination that appears after the method has established that there was none is not
    /// created twice, but substituted on the next attempt.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// This is the race the synchronization point sits in the middle of. The writer is pinned between the test of
    /// the destination and the substitution, which is precisely the window in which another process holding no lock
    /// can invalidate the decision the writer has just made.
    /// </remarks>
    [Fact]
    public async Task SubstitutesADestinationThatAppearsAfterItWasFoundAbsent()
    {
        var path = this.GetPath();

        var syncPointName = GetSyncPointName( path );
        this._syncProvider.EnableSyncPoint( syncPointName );

        var writer = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, _newContent ) );

        // The first attempt has found no destination and is about to create one.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        File.WriteAllText( path, _previousContent );

        this._syncProvider.ReleaseSyncPoint( syncPointName );

        // Reaching the synchronization point a second time is what proves that the first attempt failed rather than
        // overwriting the file that appeared without having tested for it.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        this._syncProvider.DisableSyncPoint( syncPointName );
        await this.WithTimeout( writer );

        Assert.Equal( _newContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that two writers substituting the same destination at the same time leave it holding one of the
    /// two contents in full, and leave nothing else behind.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// This method provides no mutual exclusion between writers, and does not claim to: what it guarantees is that
    /// no reader ever sees a partial file. Two writers therefore produce one of the two contents, never a mixture,
    /// and neither leaks a temporary file.
    /// </remarks>
    [Fact]
    public async Task ConcurrentWritersLeaveOneWholeContent()
    {
        var path = this.GetPath();
        var firstContent = new string( 'a', 256 * 1024 );
        var secondContent = new string( 'b', 256 * 1024 );

        var syncPointName = GetSyncPointName( path );
        this._syncProvider.EnableSyncPoint( syncPointName );

        var firstWriter = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, firstContent ) );
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        var secondWriter = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, secondContent ) );
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        // Both have written their temporary file and are about to substitute it.
        this._syncProvider.DisableSyncPoint( syncPointName );

        await this.WithTimeout( Task.WhenAll( firstWriter, secondWriter ) );

        var finalContent = File.ReadAllText( path );
        Assert.True(
            finalContent == firstContent || finalContent == secondContent,
            $"The destination holds neither content in full: {finalContent.Length} characters." );

        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that a substitution that never succeeds leaves no temporary file behind.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// The retry gives up eventually, and the attempt that gives up must clean up after itself like the others.
    /// The condition is held for the whole operation by a reader that is never closed until it has failed.
    /// </remarks>
    [SkippableFact]
    public async Task ASubstitutionThatKeepsFailingLeavesNoTemporaryFile()
    {
        // Skipped rather than returned from, so that a run on a platform where this cannot be exercised reports a
        // skip instead of a pass. See the remarks of RetriesTheSubstitutionWhileAReaderHoldsTheDestination: on
        // Unix an open descriptor does not prevent the substitution, so it cannot be made to fail this way.
        Skip.IfNot( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ), "The substitution can only be made to fail on Windows." );

        var path = this.GetPath();
        File.WriteAllText( path, _previousContent );

        using ( new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
        {
            var writer = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, _newContent ) );

            await this.WithTimeout( Assert.ThrowsAsync<IOException>( () => writer ) );
        }

        // The previous content is intact and the temporary file of every attempt has been removed.
        Assert.Equal( _previousContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }

    /// <summary>
    /// Verifies that the substitution is retried, and eventually succeeds, while a reader holds the destination
    /// open in a way that prevents it from being replaced.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// <para>
    /// This is the race that made the retry necessary. A reader that opens the destination with
    /// <see cref="FileShare.Read"/> allows neither writing nor deleting, and <c>ReplaceFile</c> needs to delete the
    /// destination, so the substitution fails with an <see cref="IOException"/> until the reader has closed.
    /// </para>
    /// <para>
    /// The condition is specific to Windows. On Unix the substitution is a <c>rename</c>, which an open descriptor
    /// does not prevent, so there is nothing to retry and nothing to assert.
    /// </para>
    /// </remarks>
    [SkippableFact]
    public async Task RetriesTheSubstitutionWhileAReaderHoldsTheDestination()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform( OSPlatform.Windows ),
            "On Unix the substitution is a rename, which an open descriptor does not prevent." );

        var path = this.GetPath();
        File.WriteAllText( path, _previousContent );

        var syncPointName = GetSyncPointName( path );
        this._syncProvider.EnableSyncPoint( syncPointName );

        var writer = RunOnDedicatedThreadAsync( () => this._fileSystem.WriteAllTextAtomically( path, _newContent ) );

        // The first attempt has written its temporary file and is about to substitute it.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        using ( var reader = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
        {
            this._syncProvider.ReleaseSyncPoint( syncPointName );

            // Reaching the synchronization point a second time is what proves that the first attempt failed and
            // that the operation was retried. Nothing else in the method reaches this point twice.
            await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

            // The reader has not been disturbed by the failed attempt.
            using ( var streamReader = new StreamReader( reader ) )
            {
                Assert.Equal( _previousContent, streamReader.ReadToEnd() );
            }
        }

        // The number of further attempts is not known, so the point is disabled rather than released.
        this._syncProvider.DisableSyncPoint( syncPointName );
        await this.WithTimeout( writer );

        Assert.Equal( _newContent, File.ReadAllText( path ) );
        Assert.Equal( new[] { Path.GetFileName( path ) }, this.GetFileNames() );
    }
}
