// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

// ReSharper disable StringLiteralTypo
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global 
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedType.Global
// Resharper disable MemberCanBePrivate.Global
// Resharper disable ClassNeverInstantiated.Global
// Resharper disable UnusedMember.Global
// Resharper disable UnusedMember.Local

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class CollectionSerializersTests : SerializationTestsBase
    {
        [Fact]
        public void ListSerializer_Ints()
        {
            this.TestValue(
                new List<int>
                {
                    2,
                    10,
                    30,
                    int.MinValue,
                    int.MaxValue,
                    -2,
                    0
                } );
        }

        [Fact]
        public void ListSerializer_Strings()
        {
            this.TestValue(
                new List<string?>
                {
                    string.Empty,
                    null,
                    "text",
                    string.Empty,
                    "2"
                } );
        }

        [Fact]
        public void ListSerializer_Classes()
        {
            this.TestValue( new List<SimpleType> { new() { Name = "X" }, new() { Name = "Y" } } );
        }

        [Fact]
        public void DictionarySerializer_IntsWithInts()
        {
            this.TestValue(
                new Dictionary<int, int>
                {
                    { 1, 1 },
                    { 2, 3 },
                    { 3, 5 },
                    { 4, 3 },
                    { 5, int.MinValue }
                } );
        }

        [Fact]
        public void DictionarySerializer_StringsWithStrings()
        {
            this.TestValue(
                new Dictionary<string, string?>
                {
                    { "a", "xx uu " },
                    { "óó&#@!`", " " },
                    { "b", null },
                    { string.Empty, "it is empty" },
                    { "very long and unpredictable text", "" }
                } );
        }

        [Fact]
        public void DictionarySerializer_IntsWithLists()
        {
            var dictionary = new Dictionary<int, List<SimpleType>>
            {
                { 1, new List<SimpleType> { new() { Name = "q" }, new() { Name = "w" } } },
                { 2, new List<SimpleType> { new() { Name = "e" }, new() { Name = "r" }, new() { Name = "y" } } }
            };

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( dictionary.Keys, deserialized.Keys );

            foreach ( var pair in dictionary )
            {
                Assert.Equal( pair.Value, deserialized[pair.Key] );
            }
        }

        [Fact]
        public void DictionarySerializer_StringsWithArraysWithCycle()
        {
            var a = new object?[2];
            var b = new object?[2];
            var dictionary = new Dictionary<string, object?[]>( 2 );

            a[0] = new SimpleType { Name = "single" };
            a[1] = dictionary;

            b[1] = null;
            b[0] = dictionary;

            dictionary["first"] = a;
            dictionary["second"] = b;

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( a.Length, deserialized["first"].Length );
            Assert.Equal( b.Length, deserialized["second"].Length );
            Assert.Same( deserialized, deserialized["first"][1] );
            Assert.Same( deserialized, deserialized["second"][0] );
        }

        [Fact]
        public void DictionarySerializer_WithStringEqualityComparer()
        {
            var dictionary = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase ) { ["first"] = "a", ["second"] = "b" };

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( dictionary["First"], deserialized["first"] );
            Assert.Equal( dictionary["first"], deserialized["First"] );
            Assert.Equal( dictionary["second"], deserialized["Second"] );
            Assert.Equal( dictionary["Second"], deserialized["second"] );
        }

        [Fact]
        public void DictionarySerializer_WithCustomEqualityComparer()
        {
            var dictionary = new Dictionary<string, string>( new CustomEqualityComparer() ) { ["first"] = "a", ["second"] = "b" };

            var deserialized = this.SerializeDeserialize( dictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( dictionary.Count, deserialized.Count );
            Assert.Equal( dictionary["first"], deserialized["f_"] );
            Assert.Equal( dictionary["f__"], deserialized["fasd"] );
            Assert.Equal( dictionary["second"], deserialized["sqwe"] );
            Assert.Equal( dictionary["sdfg"], deserialized["second"] );
        }

        [Fact]
        public void ClassSerializer_ClassContainingDictionary()
        {
            var typeWithDictionary = new TypeWithDictionary<int, string> { Dictionary = new Dictionary<int, string>() };
            typeWithDictionary.Dictionary.Add( 0, "0" );
            typeWithDictionary.Dictionary.Add( 1, "1" );
            typeWithDictionary.Dictionary.Add( 2, "2" );
            typeWithDictionary.Dictionary.Add( 3, "3" );

            var deserialized = this.SerializeDeserialize( typeWithDictionary );

            Assert.NotNull( deserialized );
            Assert.Equal( typeWithDictionary.Dictionary.Count, deserialized.Dictionary!.Count );

            Assert.Equal( typeWithDictionary.Dictionary.Keys, deserialized.Dictionary.Keys );
            Assert.Equal( typeWithDictionary.Dictionary.Values, deserialized.Dictionary.Values );
        }

        [Fact]
        public void LongLinkedListSerialization()
        {
            var ll = new LinkedListImpl { Head = new Node<int>( 1 ) };
            var tail = ll.Head;

            for ( var i = 2; i < 10000; i++ )
            {
                tail.Next = new Node<int>( i );
                tail = tail.Next;
            }

            var deserialized = this.SerializeDeserialize( ll );

            tail = ll.Head;
            var deserializedTail = deserialized.Head;

            while ( tail != null )
            {
                Assert.Equal( tail, deserializedTail );
                tail = tail.Next;
                deserializedTail = deserializedTail!.Next;
            }
        }

        private void TestValue<T>( T value )
            where T : ICollection
        {
            var deserialized = this.SerializeDeserialize( value );

            Assert.Equal( value, deserialized );
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