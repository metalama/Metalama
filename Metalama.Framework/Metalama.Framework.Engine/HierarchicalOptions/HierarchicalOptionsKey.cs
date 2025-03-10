// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.HierarchicalOptions;

public record struct HierarchicalOptionsKey( string OptionType, SerializableDeclarationId DeclarationId, string? SyntaxTreePath = null )
{
    // It is convenient for the design-time scenario to store the syntax tree path here, but it is not serialized.

    [UsedImplicitly]
    private class Serializer : ValueTypeSerializer<HierarchicalOptionsKey>
    {
        public override void SerializeObject( HierarchicalOptionsKey obj, IArgumentsWriter constructorArguments )
        {
            constructorArguments.SetValue( nameof(obj.OptionType), obj.OptionType );
            constructorArguments.SetValue( nameof(obj.DeclarationId), obj.DeclarationId );
        }

        public override HierarchicalOptionsKey DeserializeObject( IArgumentsReader constructorArguments )
        {
            return new HierarchicalOptionsKey(
#pragma warning disable SA1101
                constructorArguments.GetValue<string>( nameof(OptionType) )!,
                constructorArguments.GetValue<SerializableDeclarationId>( nameof(DeclarationId) ) );
#pragma warning restore SA1101
        }
    }
}