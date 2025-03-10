// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable IDE0028 // Collection initialization can be simplified. 

namespace Metalama.Framework.Tests.UnitTests.Collections
{
    public sealed class SkipListDictionaryTests
    {
        private static void Repeat( Action action )
        {
            // We repeat all tests many times because skip list is a random data structure.
            // If there are problems in SkipList, increase this number temporarily.

            for ( var i = 0; i < 10; i++ )
            {
                action();
            }
        }

        [Fact]
        public void TestOrdering()
        {
            Repeat(
                () =>
                {
                    var skipList = new SkipListDictionary<int, uint>();

                    var random = new Random();

                    for ( var i = 0; i < 10000; i++ )
                    {
                        skipList[random.Next()] = 0xdeadbeef;
                    }

                    var old = int.MinValue;

                    foreach ( var pair in skipList )
                    {
                        Assert.True( old < pair.Key );
                        old = pair.Key;
                    }
                } );
        }

        [Fact]
        public void TestConflict()
        {
            Repeat(
                () =>
                {
                    var skipList = new SkipListDictionary<int, int>();

                    for ( var i = 0; i < 100; i++ )
                    {
                        skipList.Add( i, i );
                    }

                    Assert.Throws<ArgumentException>( () => skipList.Add( 20, 20 ) );
                } );
        }

        [Fact]
        public void TestCount()
        {
            Repeat(
                () =>
                {
                    var skipList = new SkipListDictionary<int, uint>();
                    Assert.Empty( skipList );
                    skipList[1] = 1;
                    Assert.Single( skipList );
                    skipList[1] = 1;
                    Assert.Single( skipList );
                    skipList[2] = 2;
                    Assert.Equal( 2, skipList.Count );
                } );
        }

        [Fact( Skip = "Buggy, so we try not to use the Remove method." )]
        public void TestRemove()
        {
            var skipList = new SkipListDictionary<int, int>();
            Assert.Empty( skipList );
            skipList.Add( 1, 1 );
            Assert.Equal( new[] { 1 }, skipList.Values );
            skipList.Add( 2, 2 );
            Assert.Equal( new[] { 1, 2 }, skipList.Values );
            skipList.Add( 3, 3 );
            Assert.True( skipList.Remove( 1 ) );
            Assert.True( skipList.Remove( 2 ) );
            Assert.True( skipList.Remove( 3 ) );
            Assert.Empty( skipList );

            // Test if we can add an element at the same position.
            skipList.Add( 1, 1 );
            skipList.Add( 2, 2 );
            skipList.Add( 3, 3 );
        }

        [Fact]
        public void TestIndexOf()
        {
            Repeat(
                () =>
                {
                    const int n = 100;
                    var skipList = new SkipListDictionary<int, int>();

                    for ( var i = 0; i < n; i++ )
                    {
                        skipList.Add( i, i );
                    }

                    for ( var j = 0; j < n; j++ )
                    {
                        Assert.Equal( j, skipList.IndexOfKey( j ) );
                    }
                } );
        }

        [Fact]
        public void TestGetByIndex()
        {
            Repeat(
                () =>
                {
                    const int n = 100;
                    var skipList = new SkipListDictionary<int, int>();

                    for ( var i = 0; i < n; i++ )
                    {
                        skipList.Add( i, i );
                    }

                    for ( var j = 0; j < n; j++ )
                    {
                        Assert.Equal( j, skipList.GetAt( j ).Key );
                    }
                } );
        }

        [Fact]
        public void TestGetByKey()
        {
            Repeat(
                () =>
                {
                    const int n = 100;
                    var skipList = new SkipListDictionary<int, int>();

                    for ( var i = 0; i < n; i++ )
                    {
                        skipList.Add( i, i );
                    }

                    for ( var j = 0; j < n; j++ )
                    {
                        Assert.Equal( j, skipList[j] );
                    }
                } );
        }

        [Fact]
        public void TestGetItemsGreaterOrEqualThan()
        {
            Repeat(
                () =>
                {
                    const int n = 10;
                    var skipList = new SkipListDictionary<int, int>();

                    for ( var i = 0; i < n; i++ )
                    {
                        skipList.Add( i * 2, i * 2 );
                    }

                    for ( var j = 0; j < n * 2; j++ )
                    {
                        var items = skipList.GetItemsGreaterOrEqualThan( j );
                        var itemsIncludingPrevious = skipList.GetItemsGreaterOrEqualThan( j, true );

                        if ( j > 18 )
                        {
                            Assert.Empty( items );
                        }
                        else
                        {
                            Assert.Equal( j + (j % 2), items.Select( x => x.Value ).Min() );
                        }

                        Assert.Equal( j - (j % 2), itemsIncludingPrevious.Select( x => x.Value ).Min() );
                    }
                } );
        }

        [Fact]
        public void TestGetClosestValue()
        {
            Repeat(
                () =>
                {
                    var skipList = new SkipListDictionary<int, int> { { 10, 10 }, { 20, 20 }, { 30, 30 } };

                    void AssertHasClosestValue( int requested, int expected )
                    {
                        Assert.True( skipList.TryGetGreatestSmallerOrEqualValue( requested, out var value ) );
                        Assert.Equal( expected, value );
                    }

                    AssertHasClosestValue( 10, 10 );
                    AssertHasClosestValue( 11, 10 );
                    AssertHasClosestValue( 19, 10 );

                    Assert.False( skipList.TryGetGreatestSmallerOrEqualValue( 0, out _ ) );
                    Assert.False( skipList.TryGetGreatestSmallerOrEqualValue( 9, out _ ) );
                } );
        }

        [Fact]
        public void TestClear()
        {
            var skipList = new SkipListDictionary<int, string> { { 1, "1" }, { 2, "2" } };
            skipList.Clear();
            Assert.Empty( skipList );
        }

        [Fact]
        public void TestIndexer()
        {
            var skipList = new SkipListDictionary<int, int>();

            skipList[1] = 4;
            skipList[2] = 8;
            Assert.Equal( 4, skipList[1] );
            Assert.Equal( 8, skipList[2] );
            Assert.Equal( 0, skipList.IndexOfKey( 1 ) );
            Assert.Equal( 1, skipList.IndexOfKey( 2 ) );
            Assert.Equal( -1, skipList.IndexOfKey( 0 ) );
            
            // ReSharper disable UsageOfDefaultStructEquality
            Assert.Equal( 0, skipList.IndexOf( new KeyValuePair<int, int>( 1, 4 ) ) );
            Assert.Equal( -1, skipList.IndexOf( new KeyValuePair<int, int>( 1, 0 ) ) );
        }

        [Fact]
        public void TestValueCollection()
        {
            var skipList = new SkipListDictionary<int, int> { [1] = 4, [2] = 8 };

            Assert.Equal( 2, skipList.Values.Count );
            Assert.Equal( 4, skipList.Values[0] );
            Assert.Equal( new[] { 4, 8 }, skipList.Values );
        }

        [Fact]
        public void TestIList()
        {
            var skipList = (IList<KeyValuePair<int, int>>) new SkipListDictionary<int, int>();
            skipList.Add( new KeyValuePair<int, int>( 1, 10 ) );
            skipList.Add( new KeyValuePair<int, int>( 2, 20 ) );
            Assert.Equal( new[] { new KeyValuePair<int, int>( 1, 10 ), new KeyValuePair<int, int>( 2, 20 ) }, skipList );
            Assert.Equal( new KeyValuePair<int, int>( 1, 10 ), skipList[0] );
        }
    }
}