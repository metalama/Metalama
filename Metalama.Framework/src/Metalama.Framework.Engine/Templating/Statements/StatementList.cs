// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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