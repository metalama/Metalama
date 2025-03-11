// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class AttributeBuilder : DeclarationBuilder, IAttributeImpl
{
    private readonly IntroducedRef<IAttribute> _ref;

    internal IAttributeData AttributeConstruction { get; }

    public AttributeBuilder( AspectLayerInstance aspectLayerInstance, IDeclaration containingDeclaration, IAttributeData attributeConstruction )
        : base( aspectLayerInstance )
    {
        this.AttributeConstruction = attributeConstruction;
        this.ContainingDeclaration = containingDeclaration;
        this._ref = new IntroducedRef<IAttribute>( this.Compilation.RefFactory );
    }

    string IDisplayable.ToDisplayString( CodeDisplayFormat? format, CodeDisplayContext? context ) => throw new NotImplementedException();

    public override bool CanBeInherited => false;

    public override IDeclaration ContainingDeclaration { get; }

    IDeclaration IDeclaration.ContainingDeclaration => this.ContainingDeclaration;

    IAttributeCollection IDeclaration.Attributes => AttributeCollection.Empty;

    public override bool IsDesignTimeObservable => false;

    public override DeclarationKind DeclarationKind => DeclarationKind.Attribute;

    public INamedType Type => this.Constructor.DeclaringType;

    public IConstructor Constructor => this.AttributeConstruction.Constructor;

    public ImmutableArray<TypedConstant> ConstructorArguments => this.AttributeConstruction.ConstructorArguments;

    public INamedArgumentList NamedArguments => this.AttributeConstruction.NamedArguments;

    public FormattableString FormatPredecessor( ICompilation compilation ) => $"attribute of type '{this.Type}' on '{this.ContainingDeclaration}'";

    Location? IAspectPredecessorImpl.GetDiagnosticLocation( Compilation compilation ) => null;

    int IAspectPredecessorImpl.TargetDeclarationDepth => this.ContainingDeclaration.Depth + 1;

    [Memo]
    public override SyntaxTree PrimarySyntaxTree
        => this.ContainingDeclaration.GetPrimarySyntaxTree() ?? this.Compilation.PartialCompilation.SyntaxTreeForCompilationLevelAttributes;

    int IAspectPredecessor.PredecessorDegree => 0;

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.ContainingDeclaration.ToRef();

    ImmutableArray<AspectPredecessor> IAspectPredecessor.Predecessors => ImmutableArray<AspectPredecessor>.Empty;

    ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;

    protected override void EnsureReferenceInitialized()
    {
        this._ref.BuilderData = new AttributeBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public AttributeBuilderData BuilderData => (AttributeBuilderData) this._ref.BuilderData;

    public new IRef<IAttribute> ToRef() => this._ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => throw new NotSupportedException();
}