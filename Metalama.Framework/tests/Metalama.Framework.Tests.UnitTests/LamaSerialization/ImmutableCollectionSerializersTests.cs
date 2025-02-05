// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

// ReSharper disable StringLiteralTypo
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global 

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class ImmutableCollectionSerializersTests : SerializationTestsBase
    {
        [Fact]
        public void ImmutableArray_Ints()
        {
            this.TestValue(
                ImmutableArray.Create(
                    2,
                    10,
                    30,
                    int.MinValue,
                    int.MaxValue,
                    -2,
                    0 ) );
        }

        [Fact]
        public void ImmutableArray_Strings()
        {
            this.TestValue(
                ImmutableArray.Create(
                    string.Empty,
                    null,
                    "text",
                    string.Empty,
                    "2" ) );
        }

        [Fact]
        public void ImmutableArray_Classes()
        {
            this.TestValue( ImmutableArray.Create( new SimpleType { Name = "X" }, new SimpleType { Name = "Y" } ) );
        }

        [Fact]
        public void ImmutableDictionary__IntsWithInts()
        {
            this.TestValue(
                new Dictionary<int, int>
                {
                    { 1, 1 },
                    { 2, 3 },
                    { 3, 5 },
                    { 4, 3 },
                    { 5, int.MinValue }
                }.ToImmutableDictionary() );
        }

        [Fact]
        public void ImmutableDictionary_StringsWithStrings()
        {
            this.TestValue(
                new Dictionary<string, string?>
                {
                    { "a", "xx uu " },
                    { "óó&#@!`", " " },
                    { "b", null },
                    { string.Empty, "it is empty" },
                    { "very long and unpredictable text", "" }
                }.ToImmutableDictionary() );
        }

        [Fact]
        public void ImmutableDictionary_WithStringEqualityComparer()
        {
            var dictionary = new Dictionary<string, string>() { ["first"] = "a", ["second"] = "b" }.ToImmutableDictionary( StringComparer.OrdinalIgnoreCase );

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( dictionary["First"], deserialized["first"] );
            Assert.Equal( dictionary["first"], deserialized["First"] );
            Assert.Equal( dictionary["second"], deserialized["Second"] );
            Assert.Equal( dictionary["Second"], deserialized["second"] );
        }

        [Fact]
        public void ImmutableDictionary_WithCustomEqualityComparer()
        {
            var dictionary = new Dictionary<string, string>() { ["first"] = "a", ["second"] = "b" }.ToImmutableDictionary( new CustomEqualityComparer() );

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( dictionary["first"], deserialized["first"] );
            Assert.Equal( dictionary["f__"], deserialized["fasd"] );
            Assert.Equal( dictionary["second"], deserialized["sqwe"] );
            Assert.Equal( dictionary["sdfg"], deserialized["second"] );
        }

        [Fact]
        public void ImmutableHash_StringsWithStrings()
        {
            this.TestValue( ImmutableHashSet.Create( "un", "deux", "trois" ) );
        }

        [Fact]
        public void ImmutableHash_WithStringEqualityComparer()
        {
            var hashSet = ImmutableHashSet.Create( "un", "deux", "trois" ).WithComparer( StringComparer.InvariantCultureIgnoreCase );
            var deserialized = this.TestValue( hashSet );

#pragma warning disable xUnit2017
            Assert.True( deserialized.Contains( "UN" ) );
            Assert.False( deserialized.Contains( "quatre" ) );
#pragma warning restore xUnit2017
        }

        [Fact]
        public void ImmutableHash_WithCustomEqualityComparer()
        {
            var hashSet = ImmutableHashSet.Create( "un", "deux", "trois" ).WithComparer( new CustomEqualityComparer() );
            var deserialized = this.TestValue( hashSet );

#pragma warning disable xUnit2017
            Assert.True( deserialized.Contains( "u" ) );
            Assert.False( deserialized.Contains( "q" ) );
#pragma warning restore xUnit2017
        }

        private T TestValue<T>( T value )
            where T : ICollection
        {
            var deserialized = this.SerializeDeserialize( value );

            Assert.Equal( value.Count, deserialized.Count );

            // ReSharper disable once NotDisposedResource
            var enumerator1 = value.GetEnumerator();
            
            // ReSharper disable once NotDisposedResource
            var enumerator2 = value.GetEnumerator();

            while ( enumerator1.MoveNext() )
            {
                Assert.True( enumerator2.MoveNext() );

                Assert.Equal( enumerator1.Current, enumerator2.Current );
            }

            Assert.False( enumerator2.MoveNext() );

            return deserialized;
        }

        public sealed class SimpleType : IEquatable<SimpleType>
        {
            public string? Name { get; set; }

            public bool Equals( SimpleType? other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }

                return StringComparer.Ordinal.Equals( this.Name, other.Name );
            }

            public override bool Equals( object? obj )
            {
                if ( ReferenceEquals( null, obj ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, obj ) )
                {
                    return true;
                }

                if ( obj.GetType() != this.GetType() )
                {
                    return false;
                }

                return this.Equals( (SimpleType) obj );
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                var name = this.Name;

                return name != null ? StringComparer.Ordinal.GetHashCode( name ) : 0;
            }

            public class Serializer : ReferenceTypeSerializer<SimpleType>
            {
                public override SimpleType CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new SimpleType();
                }

                public override void SerializeObject( SimpleType obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
                {
                    initializationArguments.SetValue( "_", obj.Name );
                }

                public override void DeserializeFields( SimpleType obj, IArgumentsReader initializationArguments )
                {
                    obj.Name = initializationArguments.GetValue<string>( "_" );
                }
            }
        }

        public sealed class CustomEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals( string? x, string? y )
            {
                if ( string.IsNullOrEmpty( x ) && string.IsNullOrEmpty( y ) )
                {
                    return true;
                }

                if ( string.IsNullOrEmpty( x ) || string.IsNullOrEmpty( y ) )
                {
                    return false;
                }

                // ReSharper disable RedundantSuppressNullableWarningExpression
                return x!.StartsWith( y![0].ToString(), StringComparison.Ordinal );

                // ReSharper restore RedundantSuppressNullableWarningExpression
            }

            public int GetHashCode( string obj )
            {
                return string.IsNullOrEmpty( obj ) ? 0 : obj[0];
            }

            public class Serializer : ISerializer
            {
                public object Convert( object value, Type targetType )
                {
                    return value;
                }

                public object CreateInstance( Type type, IArgumentsReader constructorArguments )
                {
                    return new CustomEqualityComparer();
                }

                public void DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) { }

                public void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter? initializationArguments ) { }

                public bool IsTwoPhase => false;
            }
        }

        public sealed class TypeWithDictionary<TKey, TValue>
            where TKey : notnull
        {
            public Dictionary<TKey, TValue>? Dictionary { get; set; }

            public class Serializer : ReferenceTypeSerializer<TypeWithDictionary<TKey, TValue>>
            {
                public override TypeWithDictionary<TKey, TValue> CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new TypeWithDictionary<TKey, TValue>();
                }

                public override void SerializeObject(
                    TypeWithDictionary<TKey, TValue> obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments )
                {
                    initializationArguments.SetValue( "_", obj.Dictionary );
                }

                public override void DeserializeFields( TypeWithDictionary<TKey, TValue> obj, IArgumentsReader initializationArguments )
                {
                    obj.Dictionary = initializationArguments.GetValue<Dictionary<TKey, TValue>>( "_" );
                }
            }
        }

        public sealed class LinkedListImpl
        {
            public Node<int>? Head { get; set; }

            // deliberately serializing object graph not array to test deep object graphs
            public class Serializer : ReferenceTypeSerializer<LinkedListImpl>
            {
                public override LinkedListImpl CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new LinkedListImpl();
                }

                public override void SerializeObject( LinkedListImpl obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
                {
                    initializationArguments.SetValue( "_", obj.Head );
                }

                public override void DeserializeFields( LinkedListImpl obj, IArgumentsReader initializationArguments )
                {
                    obj.Head = initializationArguments.GetValue<Node<int>>( "_" );
                }
            }
        }

        public sealed class Node<T> : IEquatable<Node<T>>
            where T : notnull
        {
            public T Value { get; set; }

            public Node<T>? Next { get; set; }

            public Node( T value )
            {
                this.Value = value;
            }

            public class Serializer : ReferenceTypeSerializer<Node<T>>
            {
                public override Node<T> CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new Node<T>( constructorArguments.GetValue<T>( "v" )! );
                }

                public override void SerializeObject( Node<T> obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
                {
                    constructorArguments.SetValue( "v", obj.Value );
                    initializationArguments.SetValue( "next", obj.Next );
                }

                public override void DeserializeFields( Node<T> obj, IArgumentsReader initializationArguments )
                {
                    obj.Next = initializationArguments.GetValue<Node<T>>( "next" );
                }
            }

            public bool Equals( Node<T>? other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }

                return EqualityComparer<T>.Default.Equals( this.Value, other.Value );
            }

            public override bool Equals( object? obj )
            {
                if ( ReferenceEquals( null, obj ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, obj ) )
                {
                    return true;
                }

                if ( obj.GetType() != this.GetType() )
                {
                    return false;
                }

                return this.Equals( (Node<T>) obj );
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return EqualityComparer<T>.Default.GetHashCode( this.Value );
            }
        }
    }
}