// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

public partial class IncrementalHashSet<T>
{
    private class Serializer : ReferenceTypeSerializer<IncrementalHashSet<T>>
    {
        public override IncrementalHashSet<T> CreateInstance( IArgumentsReader constructorArguments )
        {
            var clear = constructorArguments.GetValue<bool>( "clear" );

            return new IncrementalHashSet<T>( null!, clear );
        }

        public override void SerializeObject( IncrementalHashSet<T> obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( "clear", obj._clear );
            initializationArguments.SetValue( "items", obj._dictionary );
        }

        public override void DeserializeFields( IncrementalHashSet<T> obj, IArgumentsReader initializationArguments )
        {
            obj._dictionary = initializationArguments.GetValue<ImmutableDictionary<T, bool>>( "items" )!;
        }
    }
}