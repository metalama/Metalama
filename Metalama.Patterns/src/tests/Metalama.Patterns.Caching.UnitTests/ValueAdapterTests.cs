// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class ValueAdapterTests : BaseCachingTests, IDisposable
    {
        private readonly CachingTestContext<CachingBackend> _context;

        public ValueAdapterTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper )
        {
            this._context = this.InitializeTest( "Caching.Tests.ValueAdapterTests" );
        }

        public void Dispose()
        {
            this._context.Dispose();
        }

        [Fact]
        public void TestStream()
        {
            var s1 = this.MethodReturningStream();
            var buffer1 = new byte[512];
            _ = s1.Read( buffer1, 0, buffer1.Length );

            Assert.IsType<MemoryStream>( s1 );

            var s2 = this.MethodReturningStream();
            var buffer2 = new byte[512];
            _ = s2.Read( buffer2, 0, buffer2.Length );

            CompareArrays( buffer1, buffer2 );
        }

        [Fact]
        public async Task TestStreamAsync()
        {
            var s1 = await this.MethodReturningStreamAsync();
            var buffer1 = new byte[512];

            // [Porting] Won't fix, can't be certain of original intent.
            // ReSharper disable once MethodHasAsyncOverload
            _ = s1.Read( buffer1, 0, buffer1.Length );

            Assert.IsType<MemoryStream>( s1 );

            var s2 = await this.MethodReturningStreamAsync();
            var buffer2 = new byte[512];

            // [Porting] Won't fix, can't be certain of original intent.
            // ReSharper disable once MethodHasAsyncOverload
            _ = s2.Read( buffer2, 0, buffer2.Length );

            CompareArrays( buffer1, buffer2 );
        }

        [Fact]
        public void TestEnumerable()
        {
            var a1 = this.MethodReturningEnumerable().ToArray();
            var a2 = this.MethodReturningEnumerable().ToArray();

            CompareArrays( a1, a2 );
        }

        [Fact]
        public void TestEnumerator()
        {
            var e1 = this.MethodReturningEnumerator();
            e1.MoveNext();
            var e2 = this.MethodReturningEnumerator();
            e2.MoveNext();
            Assert.Equal( e1.Current, e2.Current );
        }

        private static void CompareArrays<T>( T[] buffer1, T[] buffer2 )
        {
            Assert.Equal( buffer1.Length, buffer2.Length );

            for ( var i = 0; i < buffer1.Length; i++ )
            {
                Assert.Equal( buffer1[i], buffer2[i] );
            }
        }

        [Cache]
        private Stream MethodReturningStream()
        {
            return File.OpenRead( this.GetType().Assembly.Location );
        }

        [Cache]
        private async Task<Stream> MethodReturningStreamAsync()
        {
            await Task.Yield();

            return File.OpenRead( this.GetType().Assembly.Location );
        }

        // ReSharper disable MemberCanBeMadeStatic.Local
#pragma warning disable CA1822

        [Cache]
        private IEnumerable<int> MethodReturningEnumerable()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        [Cache]
        private IEnumerator<int> MethodReturningEnumerator()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        // ReSharper restore MemberCanBeMadeStatic.Local
#pragma warning restore CA1822
    }
}