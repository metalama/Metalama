// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Formatters;
using Xunit;

// ReSharper disable RedundantTypeDeclarationBody

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class FormattersTests
    {
        [Fact]
        public void TestSameClass()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance, b => b.AddFormatter( r => new DogFormatter( r ) ) );

            AssertKey( formatters, "FormattedDog", new Dog() );
        }

        private static void AssertKey( FormatterRepository formatters, string expectedKey, object o )
        {
            var cacheKeyBuilder = new CacheKeyBuilder( formatters, new CacheKeyBuilderOptions() );
            var key = cacheKeyBuilder.BuildDependencyKey( o );

            Assert.Equal( expectedKey, key );
        }

        [Fact]
        public void TestSameClassOverwritingFormatter()
        {
            var formatters = FormatterRepository.Create(
                CacheKeyFormatting.Instance,
                b =>
                {
                    b.AddFormatter( r => new AnimalFormatter( r ) );
                    b.AddFormatter( r => new DogFormatter( r ) );
                } );

            AssertKey( formatters, "FormattedDog", new Dog() );
        }

        [Fact]
        public void TestDerivedClass()
        {
            var formatters = FormatterRepository.Create(
                CacheKeyFormatting.Instance,
                b => b.AddFormatter( r => new DogFormatter( r ) ) );

            AssertKey( formatters, "FormattedDog", new Chihuahua() );
        }

        [Fact]
        public void TestInterface()
        {
            var formatters = FormatterRepository.Create(
                CacheKeyFormatting.Instance,
                b => b.AddFormatter( r => new AnimalFormatter( r ) ) );

            AssertKey( formatters, "FormattedAnimal", new Cat() );
        }

        [Fact]
        public void TestManuallyFormatted()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance );

            AssertKey( formatters, "ManuallyFormatted:Caching", new ManuallyFormatted() );
        }

        private interface IAnimal { }

        private sealed class Cat : IAnimal { }

        private class Dog : IAnimal { }

        private sealed class Chihuahua : Dog { }

        private sealed class AnimalFormatter : Formatter<IAnimal>
        {
            public AnimalFormatter( IFormatterRepository repository ) : base( repository ) { }

            public override void Format( UnsafeStringBuilder stringBuilder, IAnimal? value )
            {
                stringBuilder.Append( "FormattedAnimal" );
            }
        }

        private sealed class DogFormatter : Formatter<Dog>
        {
            public DogFormatter( IFormatterRepository repository ) : base( repository ) { }

            public override void Format( UnsafeStringBuilder stringBuilder, Dog? value )
            {
                stringBuilder.Append( "FormattedDog" );
            }
        }

        private sealed class ManuallyFormatted : IFormattable<CacheKeyFormatting>
        {
            void IFormattable<CacheKeyFormatting>.Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
            {
                stringBuilder.Append( "ManuallyFormatted:" + formatterRepository.Role.Name );
            }
        }
    }
}