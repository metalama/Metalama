// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class TypedConstantRefSerializer : ValueTypeSerializer<TypedConstantRef>
{
    public override void SerializeObject( TypedConstantRef obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "value", obj.RawValue );
        constructorArguments.SetValue( "type", obj.Type );
    }

    public override TypedConstantRef DeserializeObject( IArgumentsReader constructorArguments )
    {
        var value = constructorArguments.GetValue<object?>( "value" );
        var type = constructorArguments.GetValue<IRef<IType>>( "type" );

        return new TypedConstantRef( value, type );
    }
}