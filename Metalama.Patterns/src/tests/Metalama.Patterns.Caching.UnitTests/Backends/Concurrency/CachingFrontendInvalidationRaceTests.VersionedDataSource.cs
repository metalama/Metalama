// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class CachingFrontendInvalidationRaceTests
    {
        /// <summary>
        /// A data source whose data is a version number, with cached methods that return the version that they read.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each computation of a cached method reads the version, adds the dependency <see cref="_dependencyKey"/> to the
        /// current caching context, and returns the version that it read. The computations are numbered from 1 in the
        /// order in which they read the version. The computation whose number is given to the constructor blocks after it
        /// has read the version, until the test calls <see cref="ReleaseComputation"/>. The other computations do not block.
        /// </para>
        /// <para>
        /// The blocked computation waits at most <see cref="_timeout"/>, and then throws a <see cref="TimeoutException"/>.
        /// The timeout only detects a test that does not release the computation.
        /// </para>
        /// </remarks>
        private sealed class VersionedDataSource : IDisposable
        {
            /// <summary>
            /// The number of the computation that blocks.
            /// </summary>
            private readonly int _blockedComputation;

            /// <summary>
            /// Released by the blocked computation when it has read the version.
            /// </summary>
            private readonly SemaphoreSlim _computationBlockedSignal = new( 0 );

            /// <summary>
            /// Released by the test to let the blocked computation continue.
            /// </summary>
            private readonly SemaphoreSlim _computationReleasedSignal = new( 0 );

            /// <summary>
            /// The current version of the data.
            /// </summary>
            private int _version = 1;

            /// <summary>
            /// The number of computations that have started.
            /// </summary>
            private int _computationCount;

            /// <summary>
            /// The version that the blocked computation read, or zero when the blocked computation has not started.
            /// </summary>
            private int _blockedComputationVersion;

            /// <summary>
            /// Initializes a new instance of the <see cref="VersionedDataSource"/> class. The data starts at version 1.
            /// </summary>
            /// <param name="blockedComputation">The number of the computation that blocks, counted from 1.</param>
            public VersionedDataSource( int blockedComputation )
            {
                this._blockedComputation = blockedComputation;
            }

            /// <summary>
            /// Gets or sets the current version of the data.
            /// </summary>
            public int Version
            {
                get => Volatile.Read( ref this._version );
                set => Volatile.Write( ref this._version, value );
            }

            /// <summary>
            /// Gets the number of computations that have started.
            /// </summary>
            public int ComputationCount => Volatile.Read( ref this._computationCount );

            /// <summary>
            /// Gets the version that the blocked computation read, or zero when the blocked computation has not started.
            /// </summary>
            public int BlockedComputationVersion => Volatile.Read( ref this._blockedComputationVersion );

            /// <summary>
            /// Returns the current version of the data. The result is cached with the profile of the tests, and depends on
            /// <see cref="_dependencyKey"/>.
            /// </summary>
            /// <returns>The version that the computation read.</returns>
            [Cache( ProfileName = _profileName )]
            public int GetVersion()
            {
                var version = this.ReadVersion( out var mustBlock );

                if ( mustBlock )
                {
                    this.WaitForRelease();
                }

                return version;
            }

            /// <summary>
            /// Asynchronously returns the current version of the data. The result is cached with the profile of the tests,
            /// and depends on <see cref="_dependencyKey"/>.
            /// </summary>
            /// <param name="cancellationToken">A token that cancels the wait of the blocked computation.</param>
            /// <returns>The version that the computation read.</returns>
            [Cache( ProfileName = _profileName )]
            public async Task<int> GetVersionAsync( CancellationToken cancellationToken )
            {
                var version = this.ReadVersion( out var mustBlock );

                if ( mustBlock )
                {
                    await this.WaitForReleaseAsync( cancellationToken );
                }

                return version;
            }

            /// <summary>
            /// Returns the current version of the data. The result is cached with the profile of the tests and with
            /// automatic reload, and depends on <see cref="_dependencyKey"/>.
            /// </summary>
            /// <remarks>
            /// When the cache item is removed, the automatic reload computes the value again in the background and stores
            /// it. Each refresh is a computation, so the refresh can be the blocked computation.
            /// </remarks>
            /// <returns>The version that the computation read.</returns>
            [Cache( ProfileName = _profileName, AutoReload = true )]
            public int GetVersionWithAutoReload()
            {
                var version = this.ReadVersion( out var mustBlock );

                if ( mustBlock )
                {
                    this.WaitForRelease();
                }

                return version;
            }

            /// <summary>
            /// Waits until the blocked computation has read the version. The timeout only detects a failure of the test.
            /// </summary>
            /// <param name="timeout">The maximum time to wait.</param>
            /// <returns><see langword="true"/> when the blocked computation has read the version, otherwise <see langword="false"/>.</returns>
            public bool WaitUntilComputationBlocked( TimeSpan timeout ) => this._computationBlockedSignal.Wait( timeout );

            /// <summary>
            /// Asynchronously waits until the blocked computation has read the version. The timeout only detects a failure
            /// of the test.
            /// </summary>
            /// <param name="timeout">The maximum time to wait.</param>
            /// <param name="cancellationToken">A token that cancels the wait.</param>
            /// <returns><see langword="true"/> when the blocked computation has read the version, otherwise <see langword="false"/>.</returns>
            public Task<bool> WaitUntilComputationBlockedAsync( TimeSpan timeout, CancellationToken cancellationToken )
                => this._computationBlockedSignal.WaitAsync( timeout, cancellationToken );

            /// <summary>
            /// Lets the blocked computation continue. When the blocked computation has not started yet, it does not block
            /// when it starts.
            /// </summary>
            public void ReleaseComputation() => this._computationReleasedSignal.Release();

            /// <inheritdoc />
            public void Dispose()
            {
                this._computationBlockedSignal.Dispose();
                this._computationReleasedSignal.Dispose();
            }

            /// <summary>
            /// Reads the current version, adds <see cref="_dependencyKey"/> to the current caching context and counts the
            /// computation. When the computation is the blocked computation, records the version and signals the test.
            /// </summary>
            /// <param name="mustBlock">Set to <see langword="true"/> when the computation is the blocked computation.</param>
            /// <returns>The version that the computation read.</returns>
            private int ReadVersion( out bool mustBlock )
            {
                var version = this.Version;

                CachingService.Default.AddDependency( _dependencyKey );

                var computation = Interlocked.Increment( ref this._computationCount );
                mustBlock = computation == this._blockedComputation;

                if ( mustBlock )
                {
                    Volatile.Write( ref this._blockedComputationVersion, version );
                    this._computationBlockedSignal.Release();
                }

                return version;
            }

            /// <summary>
            /// Blocks the calling thread until the test calls <see cref="ReleaseComputation"/>.
            /// </summary>
            private void WaitForRelease()
            {
                if ( !this._computationReleasedSignal.Wait( _timeout ) )
                {
                    throw new TimeoutException( "The test did not release the blocked computation." );
                }
            }

            /// <summary>
            /// Returns a task that completes when the test calls <see cref="ReleaseComputation"/>.
            /// </summary>
            /// <param name="cancellationToken">A token that cancels the wait.</param>
            /// <returns>A task that completes when the blocked computation may continue.</returns>
            private async Task WaitForReleaseAsync( CancellationToken cancellationToken )
            {
                if ( !await this._computationReleasedSignal.WaitAsync( _timeout, cancellationToken ) )
                {
                    throw new TimeoutException( "The test did not release the blocked computation." );
                }
            }
        }
    }
}
