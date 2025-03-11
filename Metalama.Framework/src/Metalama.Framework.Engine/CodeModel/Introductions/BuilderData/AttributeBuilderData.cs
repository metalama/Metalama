// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class AttributeBuilderData : DeclarationBuilderData
{
    private readonly AttributeRef _ref;

    public IFullRef<INamedType> Type { get; }

    public IFullRef<IConstructor> Constructor { get; }

    public ImmutableArray<TypedConstantRef> ConstructorArguments { get; }

    public INamedArgumentList NamedArguments { get; }

    public AttributeBuilderData( AttributeBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        // Type must be set before the Ref is created.
        this.Type = builder.AttributeConstruction.Type.ToFullRef();

        this._ref = new IntroducedAttributeRef( this );

        this.Constructor = builder.AttributeConstruction.Constructor.ToFullRef();
        this.ConstructorArguments = builder.AttributeConstruction.ConstructorArguments.SelectAsImmutableArray( a => a.ToRef() );
        this.NamedArguments = builder.AttributeConstruction.NamedArguments;
        this.Attributes = ImmutableArray<AttributeBuilderData>.Empty;
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => throw new NotSupportedException( "Cannot get an IFullRef for an Attribute." );

    protected override IRef<IDeclaration> ToDeclarationRef() => this._ref;

    public override IFullRef<INamedType> DeclaringType => throw new NotImplementedException();

    public override DeclarationKind DeclarationKind => DeclarationKind.Attribute;

    public new AttributeRef ToRef() => this._ref;
}