// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Collections
{
    public sealed class DictionaryOfListTests
    {
        [Fact]
        public void Add_GroupsValuesByKey()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "1.1" }, { 1, "1.2" }, { 2, "2.1" } };

            Assert.Equal( [1, 2], dictionary.Keys );
            Assert.Equal( ["1.1", "1.2"], dictionary[1] );
            Assert.Equal( ["2.1"], dictionary[2] );
            Assert.Equal( 2, dictionary.Count );
        }

        [Fact]
        public void AddRange_AppendsToExistingKey()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };
            dictionary.AddRange( 1, ["b", "c"] );
            dictionary.AddRange( 2, ["d"] );

            Assert.Equal( ["a", "b", "c"], dictionary[1] );
            Assert.Equal( ["d"], dictionary[2] );
        }

        /// <summary>
        /// The indexer returns an empty list for an absent key rather than throwing, matching
        /// <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> and deliberately deviating from the
        /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> contract.
        /// </summary>
        [Fact]
        public void Indexer_ReturnsEmptyForAbsentKey()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };

            Assert.Equal( ["a"], dictionary[1] );
            Assert.Empty( dictionary[2] );
        }

        /// <summary>
        /// The empty-returning indexer must not be confused with key presence: <c>ContainsKey</c> still
        /// distinguishes an absent key from one that happens to have no values.
        /// </summary>
        [Fact]
        public void Indexer_ReturningEmpty_DoesNotImplyContainsKey()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };

            Assert.Empty( dictionary[2] );
            Assert.False( dictionary.ContainsKey( 2 ) );
        }

        /// <summary>
        /// The empty-returning indexer is also visible through the base
        /// <see cref="IReadOnlyDictionary{TKey,TValue}"/>, since the same implementation satisfies both.
        /// </summary>
        [Fact]
        public void Indexer_ReturnsEmptyThroughBaseInterface()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };

            IReadOnlyDictionary<int, IReadOnlyList<string>> asBase = dictionary;

            Assert.Empty( asBase[2] );
        }

        [Fact]
        public void TryGetValue_ReportsPresenceAndNeverYieldsNull()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };

            Assert.True( dictionary.TryGetValue( 1, out var present ) );
            Assert.Equal( ["a"], present );

            Assert.False( dictionary.TryGetValue( 2, out var absent ) );
            Assert.Empty( absent );
        }

        [Fact]
        public void ContainsKey_AndIsEmpty()
        {
            var dictionary = new DictionaryOfList<int, string>();
            Assert.True( dictionary.IsEmpty );
            Assert.False( dictionary.ContainsKey( 1 ) );

            dictionary.Add( 1, "a" );

            Assert.False( dictionary.IsEmpty );
            Assert.True( dictionary.ContainsKey( 1 ) );
            Assert.False( dictionary.ContainsKey( 2 ) );
        }

        /// <summary>
        /// Enumeration yields <see cref="KeyValuePair{TKey,TValue}"/> of key and list, unlike
        /// <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/>, which yields
        /// <see cref="IGrouping{TKey,TElement}"/>.
        /// </summary>
        [Fact]
        public void Enumeration_YieldsKeyValuePairsOfLists()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" }, { 1, "b" }, { 2, "c" } };

            var pairs = dictionary.ToOrderedList( p => p.Key );

            Assert.Equal( [1, 2], pairs.SelectAsArray( p => p.Key ) );
            Assert.Equal( ["a", "b"], pairs[0].Value );
            Assert.Equal( ["c"], pairs[1].Value );
        }

        [Fact]
        public void Values_ExposesEachList()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" }, { 2, "b" } };

            Assert.Equal( ["a", "b"], dictionary.Values.SelectMany( v => v ).OrderBy( v => v ) );
        }

        [Fact]
        public void Create_FromSequenceWithValueSelector()
        {
            List<(int Key, string Value)> list = [(1, "a"), (2, "b"), (1, "c")];

            var dictionary = DictionaryOfList<int, string>.Create( list, i => i.Key, i => i.Value );

            Assert.Equal( [1, 2], dictionary.Keys );
            Assert.Equal( ["a", "c"], dictionary[1] );
        }

        [Fact]
        public void ToDictionaryOfList_WithKeySelectorOnly()
        {
            List<(int, string)> list = [(1, "a"), (2, "b"), (1, "c")];

            var dictionary = list.ToDictionaryOfList( i => i.Item1 );

            Assert.Equal( [1, 2], dictionary.Keys );
            Assert.Equal( [(1, "a"), (1, "c")], dictionary[1] );
        }

        [Fact]
        public void ToDictionaryOfList_WithValueSelector()
        {
            List<(int, string)> list = [(1, "a"), (2, "b"), (1, "c")];

            var dictionary = list.ToDictionaryOfList( i => i.Item1, i => i.Item2 );

            Assert.Equal( [1, 2], dictionary.Keys );
            Assert.Equal( ["a", "c"], dictionary[1] );
        }

        [Fact]
        public void Freeze_BlocksFurtherModification()
        {
            var dictionary = new DictionaryOfList<int, string> { { 1, "a" } };

            Assert.False( dictionary.IsFrozen );
            dictionary.Freeze();
            Assert.True( dictionary.IsFrozen );

            Assert.Throws<InvalidOperationException>( () => dictionary.Add( 1, "b" ) );
            Assert.Throws<InvalidOperationException>( () => dictionary.Add( 2, "b" ) );
            Assert.Throws<InvalidOperationException>( () => dictionary.AddRange( 1, ["b"] ) );
            Assert.Throws<InvalidOperationException>( () => dictionary.AddRange( ["b"], _ => 1, v => v ) );

            // The contents are unchanged by the rejected attempts.
            Assert.Equal( ["a"], dictionary[1] );
        }

        /// <summary>
        /// A frozen map still reads normally, including the empty-returning indexer.
        /// </summary>
        [Fact]
        public void Freeze_LeavesReadsWorking()
        {
            var frozen = DictionaryOfList<int, string>.Create( [(1, "a"), (1, "b")], i => i.Item1, i => i.Item2 ).Freeze();

            Assert.Equal( ["a", "b"], frozen[1] );
            Assert.Empty( frozen[2] );
            Assert.True( frozen.ContainsKey( 1 ) );
            Assert.Single( frozen );
        }

        /// <summary>
        /// <see cref="DictionaryOfList{TKey,TValue}.Freeze"/> returns the read-only interface so a map can be built
        /// and published in one expression.
        /// </summary>
        [Fact]
        public void Freeze_IsFluentAndReturnsTheInterface()
        {
            var published =
                new[] { (1, "a") }.ToDictionaryOfList( i => i.Item1, i => i.Item2 ).Freeze();

            Assert.Equal( ["a"], published[1] );
        }

        [Fact]
        public void Freeze_IsIdempotent()
        {
            var dictionary = new DictionaryOfList<int, string>();
            dictionary.Freeze();
            dictionary.Freeze();

            Assert.True( dictionary.IsFrozen );
        }

        /// <summary>
        /// Freezing an empty map must still reject an <c>AddRange</c> whose source is empty, i.e. the check does not
        /// depend on any item being enumerated.
        /// </summary>
        [Fact]
        public void Freeze_RejectsAddRangeWithEmptySource()
        {
            var dictionary = new DictionaryOfList<int, string>();
            dictionary.Freeze();

            Assert.Throws<InvalidOperationException>( () => dictionary.AddRange( Array.Empty<string>(), _ => 1, v => v ) );
            Assert.Throws<InvalidOperationException>( () => dictionary.AddRange( 1, [] ) );
        }

        [Fact]
        public void KeyComparer_IsHonoured()
        {
            var dictionary = new DictionaryOfList<string, int>( StringComparer.OrdinalIgnoreCase ) { { "a", 1 }, { "A", 2 } };

            Assert.Single( dictionary.Keys );
            Assert.Equal( [1, 2], dictionary["a"] );
            Assert.Equal( [1, 2], dictionary["A"] );
            Assert.Same( StringComparer.OrdinalIgnoreCase, dictionary.KeyComparer );
        }
    }
}
