// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Serialization;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

public partial class IncrementalKeyedCollection<TKey, TValue>
{
    [UsedImplicitly]
    private sealed class Serializer : ReferenceTypeSerializer<IncrementalKeyedCollection<TKey, TValue>>
    {
        public override IncrementalKeyedCollection<TKey, TValue> CreateInstance( IArgumentsReader constructorArguments )
        {
            var clear = constructorArguments.GetValue<bool>( "clear" );

            return new IncrementalKeyedCollection<TKey, TValue>( null!, clear );
        }

        public override void SerializeObject(
            IncrementalKeyedCollection<TKey, TValue> obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( "clear", obj._clear );
            initializationArguments.SetValue( "items", obj._dictionary );
        }

        public override void DeserializeFields( IncrementalKeyedCollection<TKey, TValue> obj, IArgumentsReader initializationArguments )
        {
            obj._dictionary = initializationArguments.GetValue<ImmutableDictionary<TKey, Item>>( "items" )!;
        }
    }
}