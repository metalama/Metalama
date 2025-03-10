// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class DeserializedAttributeRef : AttributeRef
{
    private readonly AttributeSerializationData _serializationData;

    public DeserializedAttributeRef( AttributeSerializationData serializationData )
    {
        this._serializationData = serializationData;
    }

    public override IRef<IDeclaration> ContainingDeclaration => this._serializationData.ContainingDeclaration;

    public override IRef<INamedType> AttributeType => this._serializationData.Type;

    public override bool TryGetTarget( CompilationModel compilation, out IAttribute attribute )
    {
        attribute = compilation.Factory.GetDeserializedAttribute( this._serializationData );

        return true;
    }

    public override bool TryGetAttributeSerializationDataKey( [NotNullWhen( true )] out object? serializationDataKey )
    {
        serializationDataKey = this._serializationData;

        return true;
    }

    public override bool TryGetAttributeSerializationData( [NotNullWhen( true )] out AttributeSerializationData? serializationData )
    {
        serializationData = this._serializationData;

        return true;
    }

    public override string Name => throw new NotSupportedException();

    protected override AttributeSyntax? AttributeSyntax => null;

    protected override int GetHashCodeCore() => this._serializationData.GetHashCode();
}