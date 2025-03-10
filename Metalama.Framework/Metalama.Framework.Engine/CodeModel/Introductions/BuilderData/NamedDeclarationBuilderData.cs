// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class NamedDeclarationBuilderData : DeclarationBuilderData
{
    protected NamedDeclarationBuilderData( INamedDeclarationBuilderImpl builder, IFullRef<IDeclaration> containingDeclaration ) : base(
        builder,
        containingDeclaration )
    {
        this.Name = builder.Name;
    }

    public string Name { get; }

    protected override InsertPosition GetInsertPosition()
    {
        switch ( this.DeclaringType )
        {
            case null:
                return new InsertPosition( this.PrimarySyntaxTree.AssertNotNull() );

            case IIntroducedRef { BuilderData: NamedDeclarationBuilderData named }:
                return new InsertPosition( InsertPositionRelation.Within, named );

            case ISymbolRef declaringType:
                return new InsertPosition(
                    InsertPositionRelation.Within,
                    (MemberDeclarationSyntax) declaringType.Symbol.GetPrimaryDeclarationSyntax().AssertNotNull() );

            default:
                throw new AssertionFailedException();
        }
    }
    
    public override string ToString() => this.Name;
}