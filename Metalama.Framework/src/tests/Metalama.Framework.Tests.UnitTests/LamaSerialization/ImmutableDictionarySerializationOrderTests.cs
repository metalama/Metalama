// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

/// <summary>
/// Verifies that the bytes of a serialized <see cref="ImmutableDictionary{TKey,TValue}"/> do not depend on the enumeration order
/// of the dictionary (issue #2248).
/// </summary>
/// <remarks>
/// The enumeration order of an <see cref="ImmutableDictionary{TKey,TValue}"/> follows the hash codes of the keys, and the hash code
/// of a string is randomized per process. A single test process uses a single hash seed, so these tests create the variation
/// explicitly: every key comparer returns the same hash code for all keys, and the dictionary then enumerates the colliding keys
/// in the order in which they were added.
/// </remarks>
public sealed class ImmutableDictionarySerializationOrderTests : SerializationTestsBase
{
    private static readonly string[] _keys = ["Delta", "Alpha", "Echo", "Charlie", "Bravo"];

    [Fact]
    public void StringKeys_SameBytesForAnyEnumerationOrder()
    {
        var forward = _keys.ToImmutableDictionary( k => k, k => k.Length, new CollidingStringComparer() );
        var backward = Enumerable.Reverse( _keys ).ToImmutableDictionary( k => k, k => k.Length, new CollidingStringComparer() );

        // Check that the test inputs actually enumerate in different orders.
        Assert.NotEqual( forward.Keys, backward.Keys );

        Assert.Equal( this.Serialize( forward ), this.Serialize( backward ) );
    }

    [Fact]
    public void DeclarationIdKeys_SameBytesForAnyEnumerationOrder()
    {
        var ids = _keys.SelectAsArray( k => new SerializableDeclarationId( "T:" + k ) );

        var forward = ids.ToImmutableDictionary( k => k, k => k.ToString(), new CollidingDeclarationIdComparer() );
        var backward = Enumerable.Reverse( ids ).ToImmutableDictionary( k => k, k => k.ToString(), new CollidingDeclarationIdComparer() );

        // Check that the test inputs actually enumerate in different orders.
        Assert.NotEqual( forward.Keys, backward.Keys );

        Assert.Equal( this.Serialize( forward ), this.Serialize( backward ) );
    }

    [Fact]
    public void StringKeys_RoundTrip()
    {
        var dictionary = Enumerable.Reverse( _keys ).ToImmutableDictionary( k => k, k => k.Length, new CollidingStringComparer() );

        var deserialized = this.SerializeDeserialize( dictionary );

        Assert.Equal( dictionary.Count, deserialized.Count );

        foreach ( var pair in dictionary )
        {
            Assert.Equal( pair.Value, deserialized[pair.Key] );
        }
    }

    private byte[] Serialize( object value )
    {
        using var testContext = this.CreateTestContext();

        var memoryStream = new MemoryStream();
        testContext.Serializer.Serialize( value, memoryStream );

        return memoryStream.ToArray();
    }

    /// <summary>
    /// An ordinal string comparer that returns the same hash code for every key.
    /// </summary>
    public sealed class CollidingStringComparer : IEqualityComparer<string>
    {
        public bool Equals( string? x, string? y ) => StringComparer.Ordinal.Equals( x, y );

        public int GetHashCode( string obj ) => 0;

        public sealed class Serializer : ISerializer
        {
            public object Convert( object value, Type targetType ) => value;

            public object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new CollidingStringComparer();

            public void DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) { }

            public void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter? initializationArguments ) { }

            public bool IsTwoPhase => false;
        }
    }

    /// <summary>
    /// A comparer of <see cref="SerializableDeclarationId"/> that returns the same hash code for every key.
    /// </summary>
    public sealed class CollidingDeclarationIdComparer : IEqualityComparer<SerializableDeclarationId>
    {
        public bool Equals( SerializableDeclarationId x, SerializableDeclarationId y ) => x.Equals( y );

        public int GetHashCode( SerializableDeclarationId obj ) => 0;

        public sealed class Serializer : ISerializer
        {
            public object Convert( object value, Type targetType ) => value;

            public object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new CollidingDeclarationIdComparer();

            public void DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) { }

            public void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter? initializationArguments ) { }

            public bool IsTwoPhase => false;
        }
    }
}
