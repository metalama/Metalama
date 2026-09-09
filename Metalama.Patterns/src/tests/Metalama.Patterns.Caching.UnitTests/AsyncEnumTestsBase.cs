// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETCOREAPP3_0_OR_GREATER
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.TestHelpers;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public abstract class AsyncEnumTestsBase : BaseCachingTests, IAsyncLifetime
{
    /// <summary>
    /// Upper bound of the wait for the blocked member, so that an enumeration that never completes fails the test
    /// instead of blocking the test run.
    /// </summary>
    private static readonly TimeSpan _disposeTimeout = TimeSpan.FromMinutes( 1 );

    private readonly CachingTestContext<CachingBackend> _context;

    /// <summary>
    /// The log of the test. The tasks that the caching aspect starts append to it and the test thread reads it,
    /// so every access is synchronized on the instance itself.
    /// </summary>
    private readonly StringBuilder _log = new();

    private bool _hasInvokedBlockedMember;

    protected TestClass Instance { get; }

    protected AsyncEnumTestsBase( ITestOutputHelper testOutputHelper ) : base( testOutputHelper )
    {
        this.Instance = new TestClass( this.Log );

        this._context = this.InitializeTest( nameof(AsyncEnumerableTests) );
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    /// <remarks>
    /// The enumeration released by <see cref="FinishBlockingTaskAsync"/> is awaited before the log is read, so
    /// that the log is complete and no append is in flight while it is being read.
    /// </remarks>
    public async Task DisposeAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource( _disposeTimeout );

        await this.FinishBlockingTaskAsync( cancellationTokenSource.Token );

        this.TestOutputHelper.WriteLine( this.GetLog() );
        this._context.Dispose();
    }

    /// <summary>
    /// Releases the task on which the blocked members of <see cref="TestClass"/> wait, then waits until the
    /// member that this releases has run to the end of its body.
    /// </summary>
    /// <remarks>
    /// The wait happens only when the test has invoked a blocked member through
    /// <see cref="BlockedCachedEnumerable"/> or <see cref="BlockedCachedEnumerator"/>. No other member appends to
    /// the log after the test method has returned.
    /// </remarks>
    protected async Task FinishBlockingTaskAsync( CancellationToken cancellationToken )
    {
        this.Instance.FinishBlockingTask();

        if ( this._hasInvokedBlockedMember )
        {
            await this.Instance.WaitForBlockedMemberAsync( cancellationToken );
        }
    }

    /// <summary>
    /// Invokes <see cref="TestClass.BlockedCachedEnumerable"/> and records that the enumeration has to be awaited
    /// before the log is read.
    /// </summary>
    protected IAsyncEnumerable<int> BlockedCachedEnumerable()
    {
        this._hasInvokedBlockedMember = true;

        return this.Instance.BlockedCachedEnumerable();
    }

    /// <summary>
    /// Invokes <see cref="TestClass.BlockedCachedEnumerator"/> and records that the enumeration has to be awaited
    /// before the log is read.
    /// </summary>
    protected IAsyncEnumerator<int> BlockedCachedEnumerator()
    {
        this._hasInvokedBlockedMember = true;

        return this.Instance.BlockedCachedEnumerator();
    }

    /// <summary>
    /// Gets the content of the log.
    /// </summary>
    protected string GetLog()
    {
        lock ( this._log )
        {
            return this._log.ToString();
        }
    }

    /// <summary>
    /// Removes the content of the log.
    /// </summary>
    protected void ClearLog()
    {
        lock ( this._log )
        {
            this._log.Clear();
        }
    }

    /// <summary>
    /// Appends an entry to the log.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected void Log( string message )
    {
        lock ( this._log )
        {
            if ( this._log.Length > 0 )
            {
                this._log.Append( '.' );
            }

            this._log.Append( message );
        }
    }

    protected async Task Iterate( IAsyncEnumerator<int> iterator )
    {
        this.Log( "I1" );

        while ( await iterator.MoveNextAsync() )
        {
            this.Log( $"I2[{iterator.Current}]" );
        }

        this.Log( "I3" );
    }

    protected sealed class TestClass
    {
        private readonly Action<string> _log;

        public TestClass( Action<string> log )
        {
            this._log = log;
        }

        private readonly TaskCompletionSource _blockingTask = new();

        /// <summary>
        /// Completed when a blocked member has run to the end of its body, or when its enumeration has been
        /// abandoned before that.
        /// </summary>
        private readonly TaskCompletionSource _blockedMemberCompleted = new();

        public void FinishBlockingTask()
        {
            this._blockingTask.TrySetResult();
        }

        /// <summary>
        /// Waits until the blocked member that the test invoked has stopped appending to the log.
        /// </summary>
        public Task WaitForBlockedMemberAsync( CancellationToken cancellationToken )
            => this._blockedMemberCompleted.Task.WaitAsync( cancellationToken );

        [Cache( IgnoreThisParameter = true )]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<int> CachedEnumerable()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this._log( "E1" );

            yield return 42;

            this._log( "E2" );

            yield return 99;

            this._log( "E3" );
        }

        [Cache( IgnoreThisParameter = true )]
        public async IAsyncEnumerable<int> BlockedCachedEnumerable()
        {
            try
            {
                this._log( "E1" );

                await this._blockingTask.Task;

                this._log( "E2" );

                yield return 42;

                this._log( "E3" );

                yield return 99;

                this._log( "E4" );
            }
            finally
            {
                this._blockedMemberCompleted.TrySetResult();
            }
        }

        [Cache( IgnoreThisParameter = true )]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerator<int> CachedEnumerator()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this._log( "E1" );

            yield return 42;

            this._log( "E2" );

            yield return 99;

            this._log( "E3" );
        }

        [Cache( IgnoreThisParameter = true )]
        public async IAsyncEnumerator<int> BlockedCachedEnumerator()
        {
            try
            {
                this._log( "E1" );

                await this._blockingTask.Task;

                this._log( "E2" );

                yield return 42;

                this._log( "E3" );

                yield return 99;

                this._log( "E4" );
            }
            finally
            {
                this._blockedMemberCompleted.TrySetResult();
            }
        }
    }
}

#endif
