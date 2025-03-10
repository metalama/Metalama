// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class TypeParameterBuilderData : NamedDeclarationBuilderData
{
    private readonly IntroducedRef<ITypeParameter> _ref;

    public int Index { get; }

    public VarianceKind Variance { get; }

    public bool AllowsRefStruct { get; }

    public bool? IsConstraintNullable { get; }

    public bool HasDefaultConstructorConstraint { get; }

    public TypeKindConstraint TypeKindConstraint { get; }

    public ImmutableArray<IRef<IType>> TypeConstraints { get; }

    public bool? IsReferenceType
    {
        get;
    }

    public bool? IsNullable { get; }

    public TypeParameterBuilderData( TypeParameterBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = new IntroducedRef<ITypeParameter>( this, containingDeclaration.RefFactory );
        this.Index = builder.Index;
        this.Variance = builder.Variance;
        this.AllowsRefStruct = builder.AllowsRefStruct;
        this.IsConstraintNullable = builder.IsConstraintNullable;
        this.HasDefaultConstructorConstraint = builder.HasDefaultConstructorConstraint;
        this.TypeConstraints = builder.TypeConstraints.SelectAsImmutableArray( t => t.ToRef() );
        this.TypeKindConstraint = builder.TypeKindConstraint;
        this.IsReferenceType = builder.IsReferenceType;
        this.IsNullable = builder.IsNullable;
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public override IFullRef<INamedType>? DeclaringType => this.ContainingDeclaration.DeclaringType;

    public new IFullRef<ITypeParameter> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.TypeParameter;
}