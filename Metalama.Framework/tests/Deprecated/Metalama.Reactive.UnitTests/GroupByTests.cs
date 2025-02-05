using System.Collections.Generic;
using System.Linq;
using Metalama.Reactive.Sources;
using Xunit;
using static Metalama.Reactive.UnitTests.TestGroupObserver.EventKind;

namespace Metalama.Reactive.UnitTests
{
    public class GroupByTests
    {
        [Fact]
        public void GroupByImmutableTest()
        {
            var source = new[] { 1, 2, 11 }.ToImmutableReactive();

            // with using System.Linq, LINQ on ReactiveHashSet above is ambiguous
            var grouped = Enumerable.ToList( source.GroupBy( i => i % 10 ).GetValue() );

            Assert.Equal( 2, grouped.Count );

            var group1 = grouped[0];
            Assert.Equal( 1, group1.Key );
            Assert.Equal( new[] { 1, 11 }, group1.GetValue() );

            var group2 = grouped[1];
            Assert.Equal( 2, group2.Key );
            Assert.Equal( new[] { 2 }, group2.GetValue() );
        }

        [Fact]
        public void EmptyGroupTest()
        {
            var groups = new[] { 1, 2, 3 }.ToImmutableReactive()
                .GroupBy( i => i % 10 );

            Assert.Empty( groups[0].GetValue() );
        }

        [Fact]
        public void TestObserver()
        {
            var source = new ReactiveHashSet<int>();

            var groups = source.GroupBy( x => x % 10 );

            Assert.Equal( new object[0], GetGroups() );

            var observer = new TestGroupObserver( groups );
            observer.AssertAndClearEvents();

            source.Add( 1 );
            observer.AssertAndClearEvents( (GroupAdded, (0, 1)), (ItemAdded, (0, 1)), (ItemsChanged, 0), (GroupsInvalidated, false) );
            Assert.Equal( new[] { new[] { 1 } }, GetGroups() );

            source.Add( 11 );
            observer.AssertAndClearEvents( (ItemAdded, (0, 11)), (ItemsChanged, 0), (GroupsInvalidated, false) );
            Assert.Equal( new[] { new[] { 1, 11 } }, GetGroups() );

            source.Add( 12 );
            observer.AssertAndClearEvents( (GroupAdded, (1, 2)), (ItemAdded, (1, 12)), (ItemsChanged, 1), (GroupsInvalidated, false) );
            Assert.Equal( new[] { new[] { 1, 11 }, new[] { 12 } }, GetGroups() );

            IEnumerable<IEnumerable<int>> GetGroups()
            {
                return groups.GetValue().Select( g => g.GetValue() );
            }
        }
    }
}