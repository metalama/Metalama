// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class AttributeSerializationDataSerializer : ReferenceTypeSerializer<AttributeSerializationData>
{
    public override AttributeSerializationData CreateInstance( IArgumentsReader constructorArguments ) => new( constructorArguments );

    public override void SerializeObject( AttributeSerializationData obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        => obj.Serialize( constructorArguments );

    public override void DeserializeFields( AttributeSerializationData obj, IArgumentsReader initializationArguments ) { }
}