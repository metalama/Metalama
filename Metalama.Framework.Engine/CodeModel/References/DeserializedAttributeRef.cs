// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class DeserializedAttributeRef( AttributeSerializationData data ) : AttributeRef
{
    public override IRef<IDeclaration> ContainingDeclaration => data.ContainingDeclaration;

    public override IRef<INamedType> AttributeType => data.Type;

    public override bool TryGetTarget( CompilationModel compilation, [NotNullWhen( true )] out IAttribute? attribute )
    {
        attribute = compilation.Factory.GetDeserializedAttribute( data );

        return true;
    }

    public override bool TryGetAttributeSerializationDataKey( [NotNullWhen( true )] out object? serializationDataKey )
    {
        serializationDataKey = data;

        return true;
    }

    public override bool TryGetAttributeSerializationData( [NotNullWhen( true )] out AttributeSerializationData? serializationData )
    {
        serializationData = data;

        return true;
    }

    public override string Name => throw new NotSupportedException();

    protected override AttributeSyntax? AttributeSyntax => null;

    protected override int GetHashCodeCore() => data.GetHashCode();
}