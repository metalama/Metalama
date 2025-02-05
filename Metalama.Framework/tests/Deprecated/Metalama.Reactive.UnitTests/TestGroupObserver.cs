using System.Collections.Generic;
using System.Linq;
using Xunit;
using static Metalama.Reactive.UnitTests.TestGroupObserver.EventKind;

// ReSharper disable ObjectCreationAsStatement

namespace Metalama.Reactive.UnitTests
{
    internal class TestGroupObserver : IReactiveCollectionObserver<IReactiveGroup<int, int>>
    {
        public enum EventKind
        {
            GroupAdded,
            GroupRemoved,
            GroupReplaced,
            GroupsChanged,
            GroupsInvalidated,
            ItemAdded,
            ItemRemoved,
            ItemReplaced,
            ItemsChanged,
            ItemsInvalidated,
        }

        private readonly List<(EventKind, object)> _events = new();

        private readonly IReactiveCollection<IReactiveGroup<int, int>> _source;

        private int _nextI;

        public TestGroupObserver( IReactiveCollection<IReactiveGroup<int, int>> source )
        {
            this._source = source;
            source.Observable.AddObserver( this );
        }

        public void AssertAndClearEvents( params (EventKind, object)[] expected )
        {
            Assert.Equal( expected, this._events );

            this._events.Clear();
        }

        public void ReconnectGroups()
        {
            foreach ( var g in this._source.GetValue() )
            {
                new GroupObserver( this, g );
            }
        }

        public void Dispose()
        {
        }

        public void OnItemAdded( IReactiveSubscription subscription, IReactiveGroup<int, int> item, int newVersion )
        {
            new GroupObserver( this, item );
        }

        public void OnItemRemoved( IReactiveSubscription subscription, IReactiveGroup<int, int> item, int newVersion )
        {
            this._events.Add( (GroupRemoved, item.Key) );
        }

        public void OnItemReplaced( IReactiveSubscription subscription, IReactiveGroup<int, int> oldItem, IReactiveGroup<int, int> newItem, int newVersion )
        {
            this._events.Add( (GroupReplaced, (oldItem.Key, newItem.Key)) );
        }

        public void OnValueChanged( IReactiveSubscription subscription, IEnumerable<IReactiveGroup<int, int>> oldValue, IEnumerable<IReactiveGroup<int, int>> newValue, int newVersion, bool isBreakingChange = false )
        {
            this._events.Add( (GroupsChanged, newValue.Select( g => g.Key ).ToArray()) );
        }

        public void OnValueInvalidated( IReactiveSubscription subscription, bool isBreakingChange )
        {
            this._events.Add( (GroupsInvalidated, isBreakingChange) );
        }

        private class GroupObserver : IReactiveCollectionObserver<int>
        {
            private readonly TestGroupObserver _parent;
            private readonly int _i;

            public GroupObserver( TestGroupObserver parent, IReactiveGroup<int, int> source )
            {
                this._parent = parent;

                this._i = parent._nextI++;

                parent._events.Add( (GroupAdded, (this._i, source.Key)) );

                source.Observable.AddObserver( this );
            }

            public void Dispose()
            {
            }

            public void OnItemAdded( IReactiveSubscription subscription, int item, int newVersion )
            {
                this._parent._events.Add( (ItemAdded, (this._i, item)) );
            }

            public void OnItemRemoved( IReactiveSubscription subscription, int item, int newVersion )
            {
                this._parent._events.Add( (ItemRemoved, (this._i, item)) );
            }

            public void OnItemReplaced( IReactiveSubscription subscription, int oldItem, int newItem, int newVersion )
            {
                this._parent._events.Add( (ItemReplaced, (this._i, (oldItem, newItem))) );
            }

            public void OnValueChanged( IReactiveSubscription subscription, IEnumerable<int> oldValue, IEnumerable<int> newValue, int newVersion, bool isBreakingChange = false )
            {
                this._parent._events.Add( (ItemsChanged, this._i) );
            }

            public void OnValueInvalidated( IReactiveSubscription subscription, bool isBreakingChange )
            {
                this._parent._events.Add( (ItemsInvalidated, (this._i, isBreakingChange)) );
            }
        }
    }
}
