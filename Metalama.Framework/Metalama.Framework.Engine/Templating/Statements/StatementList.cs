// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Templating.Statements;

internal sealed class StatementList : IStatementListImpl
{
    private readonly ImmutableArray<object> _items;

    public StatementList( ImmutableArray<object> items )
    {
        this._items = items;
    }

    public IReadOnlyList<StatementSyntax> GetSyntaxes( TemplateSyntaxFactoryImpl? templateSyntaxFactory )
    {
        var statements = new List<StatementSyntax>();

        foreach ( var item in this._items )
        {
            switch ( item )
            {
                case IStatementImpl statement:
                    statements.Add( statement.GetSyntax( templateSyntaxFactory ) );

                    break;

                case IStatementListImpl list:
                    statements.AddRange( list.GetSyntaxes( templateSyntaxFactory ) );

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected item in StatementList: {item.GetType().Name}." );
            }
        }

        return statements;
    }
}