// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// An implementation of <see cref="AttributeRef"/> based on <see cref="BuilderData"/>.
/// </summary>
internal sealed class IntroducedAttributeRef : AttributeRef
{
    public AttributeBuilderData BuilderData { get; }

    public IntroducedAttributeRef( AttributeBuilderData builderData )
    {
        this.BuilderData = builderData;
    }

    public override IRef<IDeclaration> ContainingDeclaration => this.BuilderData.ContainingDeclaration;

    public override IRef<INamedType> AttributeType => this.BuilderData.Constructor.DeclaringType.AssertNotNull();

    public override bool TryGetTarget( CompilationModel compilation, [NotNullWhen( true )] out IAttribute? attribute )
    {
        attribute = new IntroducedAttribute( this.BuilderData, compilation, GenericContext.Empty );

        return true;
    }

    public override bool TryGetAttributeSerializationDataKey( [NotNullWhen( true )] out object? serializationDataKey )
    {
        serializationDataKey = this.BuilderData;

        return true;
    }

    public override bool TryGetAttributeSerializationData( [NotNullWhen( true )] out AttributeSerializationData? serializationData )
    {
        serializationData = new AttributeSerializationData( this.BuilderData );

        return true;
    }

    public override string Name => this.BuilderData.Type.Name.AssertNotNull();

    protected override AttributeSyntax? AttributeSyntax => null;

    public override bool Equals( AttributeRef? other )
        => other is IntroducedAttributeRef builderAttributeRef && this.BuilderData.Equals( builderAttributeRef.BuilderData );

    protected override int GetHashCodeCore() => this.BuilderData.GetHashCode();
}