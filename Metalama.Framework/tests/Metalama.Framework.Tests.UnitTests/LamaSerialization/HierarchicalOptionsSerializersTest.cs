// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public sealed class HierarchicalOptionsSerializersTest : SerializationTestsBase
{
    [Fact]
    public void KeyedCollection()
    {
        var collection = IncrementalKeyedCollection.AddOrApplyChanges<string, MyItem>( new MyItem( "TheKey" ) );
        var roundtrip = this.SerializeDeserialize( collection );
        Assert.Single( roundtrip );
        Assert.Equal( "TheKey", roundtrip.Single().Key );
    }

    [Fact]
    public void HashSet()
    {
        var collection = IncrementalHashSet.Add( "TheKey" );
        var roundtrip = this.SerializeDeserialize( collection );
        Assert.Single( roundtrip );
        Assert.Equal( "TheKey", roundtrip.Single() );
    }

    private sealed class MyItem : IIncrementalKeyedCollectionItem<string>
    {
        public MyItem( string key )
        {
            this.Key = key;
        }

        public string Key { get; }

        public object ApplyChanges( object changes, in ApplyChangesContext context ) => this;

        [UsedImplicitly]
        private sealed class Serializer : ReferenceTypeSerializer<MyItem>
        {
            public override MyItem CreateInstance( IArgumentsReader constructorArguments ) => new( constructorArguments.GetValue<string>( "Key" )! );

            public override void SerializeObject( MyItem obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            {
                constructorArguments.SetValue( "Key", obj.Key );
            }

            public override void DeserializeFields( MyItem obj, IArgumentsReader initializationArguments ) { }
        }
    }
}