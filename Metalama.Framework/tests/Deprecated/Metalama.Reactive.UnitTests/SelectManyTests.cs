using System.Collections.Immutable;
using Metalama.Reactive.Sources;
using Xunit;

namespace Metalama.Reactive.UnitTests
{

    public class SelectManyTests
    {

        [Fact]
        public void SelectManyTest()
        {
            var compilation = new TestCompilation();

            var memberNames = from type in compilation.Types
                              from member in type.Members
                              select $"{type.Name}.{member.Name}";

            var baseTypeNames = from type in compilation.Types
                                from baseType in type.BaseTypes
                                select $"{type.Name} : {baseType}";

            Assert.Empty( memberNames.GetValue() );
            Assert.Empty( baseTypeNames.GetValue() );

            var c = new SourceType( "C", ImmutableList.Create( "B" ) );

            compilation.Types.Add( c );

            Assert.Empty( memberNames.GetValue() );
            Assert.Equal( new[] { "C : B" }, baseTypeNames.GetValue() );

            c.Members.Add( new Member( "M" ) );

            Assert.Equal( new[] { "C.M" }, memberNames.GetValue() );
        }

        [Fact]
        public void ImmutableNestedSelectManyTest()
        {
            var groups = new[] { new TestCompilation() }.ToImmutableReactive()
                .SelectMany( c => c.Types.SelectMany( t => t.Members ) )
                .GroupBy( a => a.Name )
                .GetValue();

            Assert.Empty( groups );
        }

        [Fact]
        public void ReactiveNestedSelectManyTest()
        {
            var groups = new ReactiveHashSet<TestCompilation> { new() }
                .SelectMany( c => c.Types.SelectMany( t => t.Members ) )
                .GroupBy( a => a.Name )
                .GetValue();

            Assert.Empty( groups );
        }

        [Fact]
        public void SelectManyReactiveWithWhereTest()
        {
            var compilation = new TestCompilation();

            compilation.Types.Add( new( "C", null ) );

            var codeElements = compilation.Types.SelectDescendants( type => type.NestedTypes.Where( _ => true ) );

            Assert.Single( codeElements.GetValue() );
        }

        [Fact]
        public void SelectManyImmutableWithWhereTest()
        {
            var types = new[] { new SourceType( "C", null ) }.ToImmutableReactive();

            var codeElements = types.SelectDescendants( type => type.NestedTypes.Where( _ => true ) );

            Assert.Single( codeElements.GetValue() );
        }
    }
}
