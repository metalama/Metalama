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
