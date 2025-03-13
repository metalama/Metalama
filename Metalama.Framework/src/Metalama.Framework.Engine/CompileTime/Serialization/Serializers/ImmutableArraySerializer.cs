// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class ImmutableArraySerializer<T> : ValueTypeSerializer<ImmutableArray<T>>
    {
        private const string _keyName = "_";

        public override void SerializeObject( ImmutableArray<T> obj, IArgumentsWriter constructorArguments )
        {
            // we need to save arrays in constructorArguments because objects from initializationArguments can be not fully deserialized when DeserializeFields is called
            constructorArguments.SetValue( _keyName, obj.ToArray() );
        }

        public override ImmutableArray<T> DeserializeObject( IArgumentsReader constructorArguments )
        {
            var values = constructorArguments.GetValue<T[]>( _keyName ).AssertNotNull();

            return ImmutableArray.Create( values );
        }
    }
}