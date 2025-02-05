// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.SyntaxBuilders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating.Statements;

internal sealed class UnwrappedBlockStatementList : IStatementListImpl
{
    private readonly IStatement _statement;

    public UnwrappedBlockStatementList( IStatement statement )
    {
        this._statement = statement;
    }

    public IReadOnlyList<StatementSyntax> GetSyntaxes( TemplateSyntaxFactoryImpl? templateSyntaxFactory )
    {
        var syntax = ((IStatementImpl) this._statement).GetSyntax( templateSyntaxFactory );

        if ( syntax is BlockSyntax block )
        {
            return GetDeepestBlock( block ).Statements;
        }
        else
        {
            return SyntaxFactory.SingletonList( syntax );
        }

        static BlockSyntax GetDeepestBlock( BlockSyntax parent )
        {
            if ( parent.Statements is [BlockSyntax innerBlock] )
            {
                return innerBlock;
            }
            else
            {
                return parent;
            }
        }
    }
}