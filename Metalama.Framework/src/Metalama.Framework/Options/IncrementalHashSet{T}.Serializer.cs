// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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