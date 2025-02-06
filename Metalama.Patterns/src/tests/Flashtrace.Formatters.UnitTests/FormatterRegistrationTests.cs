// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.UnitTests.Assets;
using System.Collections;
using System.Collections.ObjectModel;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Flashtrace.Formatters.UnitTests
{
    public class FormatterRegistrationTests : FormattersTestsBase
    {
        public FormatterRegistrationTests( ITestOutputHelper logger ) : base( logger ) { }

        [Fact]
        public void RegisterBeforeFirstGet()
        {
            var formatters = CreateRepository( b => b.AddFormatter( r => new EnumerableFormatter<int>( r ) ) );

            var afterFormatter = formatters.Get<IEnumerable<int>>();

            Assert.IsType<EnumerableFormatter<int>>( afterFormatter );

            var result = this.Format<IEnumerable<int>>( formatters, new[] { 1, 2, 3 } );

            Assert.Equal( "[1,2,3]", result );
        }

        [Fact]
        public void EnumerableIntFormatter()
        {
            this.EnumerableIntFormatterMethod( r => new EnumerableFormatter<int>( r ) );
            this.EnumerableIntFormatterMethod( r => new EnumerableFormatter<int>( r ) );
            this.EnumerableIntFormatterMethod( r => new EnumerableIntFormatter( r ) );
            this.EnumerableIntFormatterMethod( r => new EnumerableIntFormatter( r ) );
        }

        private void EnumerableIntFormatterMethod( Func<IFormatterRepository, Formatter<IEnumerable<int>>> formatterFactory )
        {
            int[] array = [1, 2, 3];

            var formatters = CreateRepository( b => b.AddFormatter( formatterFactory ) );

            Assert.Equal( "[1,2,3]", this.Format<int[]>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable<int>>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable>( formatters, array ) );
        }

        [Fact]
        public void EnumerableTFormatter()
        {
            int[] array = [1, 2, 3];

            var formatters = CreateRepository( b => b.AddFormatter( typeof(IEnumerable<>), typeof(EnumerableFormatter<>) ) );

            Assert.Equal( "[1,2,3]", this.Format<int[]>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable<int>>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable>( formatters, array ) );
        }

        [Fact]
        public void CollectionTFormatter()
        {
            var collection = new ObservableCollection<int> { 1, 2, 3 };

            var formatters = CreateRepository( b => b.AddFormatter( typeof(Collection<>), typeof(EnumerableFormatter<>) ) );

            Assert.Equal( "[1,2,3]", this.Format<ObservableCollection<int>>( formatters, collection ) );
            Assert.Equal( "[1,2,3]", this.Format<Collection<int>>( formatters, collection ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable<int>>( formatters, collection ) );
        }

        [Fact]
        public void ArrayFormatter()
        {
            int[] array = [1, 2, 3];

            var formatters = CreateRepository( b => b.AddFormatter( typeof(Array), typeof(EnumerableFormatter<>) ) );

            Assert.Equal( "[1,2,3]", this.Format<int[]>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable<int>>( formatters, array ) );
            Assert.Equal( "[1,2,3]", this.Format<IEnumerable>( formatters, array ) );
        }

        [Fact]
        public void DictionaryFormatter()
        {
            var dictionary = new Dictionary<int, string> { { 1, "uno" }, { 2, "dos" }, { 3, "tres" } };

            var formatters = CreateRepository( b => b.AddFormatter( typeof(IDictionary<,>), typeof(DictionaryFormatter<,>) ) );

            Assert.Equal( "{1:uno,2:dos,3:tres}", this.Format<Dictionary<int, string>>( formatters, dictionary ) );
            Assert.Equal( "{1:uno,2:dos,3:tres}", this.Format<IDictionary<int, string>>( formatters, dictionary ) );
            Assert.Equal( "{1:uno,2:dos,3:tres}", this.Format<IDictionary>( formatters, dictionary ) );
        }

        [Fact]
        public void NullableFormatter()
        {
            var formatters = CreateRepository( b => b.AddFormatter( typeof(int?), typeof(NullableFormatter<int>) ) );

            Assert.Equal( "<null>", this.Format<int?>( formatters, null ) );
            Assert.Equal( "2", this.Format<int>( formatters, 2 ) );
        }

        [Fact]
        public void GenericNullableFormatter()
        {
            var formatters = CreateRepository( b => b.AddFormatter( typeof(Nullable<>), typeof(NullableFormatter<>) ) );

            Assert.Equal( "<null>", this.Format<int?>( formatters, null ) );
            Assert.Equal( "2", this.Format<int>( formatters, 2 ) );
        }

        [Fact]
        public void NonNullableFormatter()
        {
            var formatters = CreateRepository( b => b.AddFormatter( typeof(int), typeof(NonNullableFormatter<int>) ) );

            Assert.Equal( "null", this.Format<int?>( formatters, null ) );
            Assert.Equal( "[2]", this.Format<int>( formatters, 2 ) );
        }

        [Fact]
        public void FormatterExceptions()
        {
            void Register( Type interfaceType, Type formatterType )
            {
                CreateRepository( b => b.AddFormatter( interfaceType, formatterType ) );
            }

            AssertEx.Throws<ArgumentException>( () => Register( typeof(int[]), typeof(EnumerableFormatter<>) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(int), typeof(EnumerableFormatter<int>) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(IEnumerable<>), typeof(EnumerableFormatter<int>) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(IEnumerable<int>), typeof(EnumerableFormatter<>) ) );
            AssertEx.Throws<MissingMethodException>( () => Register( typeof(int), typeof(int) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(IEnumerable<>), typeof(IEnumerable<>) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(Dictionary<,>), typeof(EnumerableFormatter<>) ) );

            AssertEx.Throws<ArgumentException>(
                () => CreateRepository( b => b.AddFormatter( typeof(IEnumerable<>), r => new EnumerableFormatter<int>( r ) ) ) );

            AssertEx.Throws<ArgumentException>(
                () =>
                    CreateRepository( b => b.AddFormatter( typeof(int), r => new EnumerableFormatter<int>( r ) ) ) );

            AssertEx.Throws<ArgumentException>( () => Register( typeof(int), typeof(NullableFormatter<int>) ) );
            AssertEx.Throws<ArgumentException>( () => Register( typeof(int?), typeof(NonNullableFormatter<int>) ) );
        }

        [Fact]
        public void NoVariance()
        {
            var formatters = CreateRepository( b => b.AddFormatter( r => new EnumerableFormatter<object>( r ) ) );

            string[] array = ["foo", "bar", "baz"];

            Assert.Equal( "{string[]}", this.Format<IEnumerable<string>>( formatters, array ) );
            Assert.Equal( "[foo,bar,baz]", this.Format<IEnumerable<object>>( formatters, array ) );
        }
    }
}