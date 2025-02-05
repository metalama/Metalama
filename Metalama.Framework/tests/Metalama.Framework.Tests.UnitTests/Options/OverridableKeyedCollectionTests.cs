// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Options;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Options;

public sealed class OverridableKeyedCollectionTests
{
    [Fact]
    public void Add()
    {
        Assert.Single( IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Item" ) ) );
    }

    [Fact]
    public void Replace()
    {
        var item = Assert.Single(
            IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Item", "New" ) ).AddOrApplyChanges( new Item( "Item", "Replaced" ) ) );

        Assert.Equal( "Replaced", item.Value );
    }

    [Fact]
    public void Remove()
    {
        var item = IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Item" ) ).Remove( "Item" );
        Assert.Empty( item );
    }

    [Fact]
    public void ClearThenAdd()
    {
        var collection1 = IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Item" ) );
        var collection2 = IncrementalKeyedCollection.Clear<string, Item>().AddOrApplyChanges( new Item( "Item", "New" ) );

        var merged = collection1.ApplyChanges( collection2, default );
        var item = Assert.Single( merged );

        Assert.Equal( "New", item.Value );
    }

    [Fact]
    public void OverrideWithClear()
    {
        var collection1 = IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Key" ) );
        var collection2 = IncrementalKeyedCollection.Clear<string, Item>();
        var merged = collection1.ApplyChanges( collection2, default );
        Assert.Empty( merged );
    }

    [Fact]
    public void OverrideWithUpdate()
    {
        var collection1 = IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Key" ) );
        var collection2 = IncrementalKeyedCollection.AddOrApplyChanges<string, Item>( new Item( "Key", "Updated" ) );
        var merged = collection1.ApplyChanges( collection2, default );
        Assert.Equal( "Updated", Assert.Single( merged ).Value );
    }

    private sealed class Item : IIncrementalKeyedCollectionItem<string>
    {
        public object ApplyChanges( object changes, in ApplyChangesContext context ) => new Item( this.Key, ((Item) changes).Value ?? this.Value );

        public string Key { get; }

        public string? Value { get; }

        public Item( string key, string? value = null )
        {
            this.Key = key;
            this.Value = value;
        }
    }
}