// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.SyntaxBuilders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Statements;

internal sealed class BlockStatement : IStatementImpl
{
    private readonly IStatementList _statements;

    public BlockStatement( IStatementList statements )
    {
        this._statements = statements;
    }

    public StatementSyntax GetSyntax( TemplateSyntaxFactoryImpl? templateSyntaxFactory )
        => SyntaxFactory.Block( ((IStatementListImpl) this._statements).GetSyntaxes( templateSyntaxFactory ) );
}