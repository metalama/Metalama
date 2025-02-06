// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NETCOREAPP3_0_OR_GREATER
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.TestHelpers;
using System.Text;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public abstract class AsyncEnumTestsBase : BaseCachingTests, IDisposable
{
    private readonly CachingTestContext<CachingBackend> _context;

    protected StringBuilder StringBuilder { get; }

    protected TestClass Instance { get; }

    protected AsyncEnumTestsBase( ITestOutputHelper testOutputHelper ) : base( testOutputHelper )
    {
        this.StringBuilder = new StringBuilder();
        this.Instance = new TestClass( this.Log );

        this._context = this.InitializeTest( nameof(AsyncEnumerableTests) );
    }

    public void Dispose()
    {
        this.Instance.FinishBlockingTask();
        this.TestOutputHelper.WriteLine( this.StringBuilder.ToString() );
        this._context.Dispose();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected void Log( string message )
    {
        if ( this.StringBuilder.Length > 0 )
        {
            this.StringBuilder.Append( "." );
        }

        this.StringBuilder.Append( message );
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

        public void FinishBlockingTask()
        {
            this._blockingTask.TrySetResult();
        }

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
            this._log( "E1" );

            await this._blockingTask.Task;

            this._log( "E2" );

            yield return 42;

            this._log( "E3" );

            yield return 99;

            this._log( "E4" );
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
            this._log( "E1" );

            await this._blockingTask.Task;

            this._log( "E2" );

            yield return 42;

            this._log( "E3" );

            yield return 99;

            this._log( "E4" );
        }
    }
}

#endif