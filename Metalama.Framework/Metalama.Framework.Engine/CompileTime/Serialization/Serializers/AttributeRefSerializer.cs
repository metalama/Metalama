// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class AttributeRefSerializer : ReferenceTypeSerializer
{
    public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
    {
        var data = constructorArguments.GetValue<AttributeSerializationData>( "data" ).AssertNotNull();

        return new DeserializedAttributeRef( data );
    }

    public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        var attributeRef = (AttributeRef) obj;
        var serializationContext = (ISerializationContext) constructorArguments;

        // We're trying to deduplicate instances of the AttributeSerializationData class.

        if ( !attributeRef.TryGetAttributeSerializationDataKey( out var serializationDataKey ) )
        {
            throw new AssertionFailedException( $"Cannot serialize the attribute '{attributeRef}'." );
        }

        const string contextProperty = "AttributeRefSerializer.Instances";
        Dictionary<object, AttributeSerializationData> instances;

        if ( serializationContext.ContextProperties.TryGetValue( contextProperty, out var attributeSerializationDataInstancesObj ) )
        {
            instances = (Dictionary<object, AttributeSerializationData>) attributeSerializationDataInstancesObj!;
        }
        else
        {
            instances = new Dictionary<object, AttributeSerializationData>();
            serializationContext.ContextProperties.Add( contextProperty, instances );
        }

        if ( !instances.TryGetValue( serializationDataKey, out var serializationData ) )
        {
            if ( !attributeRef.TryGetAttributeSerializationData( out serializationData ) )
            {
                throw new AssertionFailedException( $"Cannot serialize the attribute '{attributeRef}'." );
            }
        }

        constructorArguments.SetValue( "data", serializationData );
    }

    public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }
}