// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Reactive.Sources;
using Xunit;

namespace Metalama.Reactive.UnitTests
{
    public class SomeTests
    {

        [Fact]
        public void SomeTest()
        {
            var source = new ReactiveHashSet<int>();

            var some = source.Some();

            source.Add( 1 );

            Assert.Equal( 1, some.GetValue() );

            source.Replace( 1, 2 );

            Assert.Equal( 2, some.GetValue() );

            source.Add( 3 );

            Assert.Equal( 2, some.GetValue() );

            source.Remove( 2 );

            Assert.Equal( 3, some.GetValue() );
        }
    }
}
