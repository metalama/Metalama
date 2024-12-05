// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverridePropertyOrIndexerTransformation : OverrideMemberTransformation
{
    protected OverridePropertyOrIndexerTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IPropertyOrIndexer> overriddenPropertyOrIndexer )
        : base( aspectLayerInstance, overriddenPropertyOrIndexer ) { }

    /// <summary>
    /// Creates a trivial passthrough body for cases where we have template only for one accessor kind.
    /// </summary>
    protected BlockSyntax CreateIdentityAccessorBody( MemberInjectionContext context, SyntaxKind accessorDeclarationKind )
    {
        var proceedExpression = accessorDeclarationKind switch
        {
            SyntaxKind.GetAccessorDeclaration => this.CreateProceedGetExpression( context ),
            SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration => this.CreateProceedSetExpression( context ),
            _ => throw new AssertionFailedException( $"Unexpected SyntaxKind: {accessorDeclarationKind}." )
        };

        return TransformationHelper.CreateIdentityAccessorBody(
            accessorDeclarationKind,
            proceedExpression,
            context.SyntaxGenerationContext );
    }

    protected abstract ExpressionSyntax CreateProceedGetExpression( MemberInjectionContext context );

    protected abstract ExpressionSyntax CreateProceedSetExpression( MemberInjectionContext context );
}