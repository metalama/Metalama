// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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