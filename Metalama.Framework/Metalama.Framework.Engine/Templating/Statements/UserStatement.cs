// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Statements
{
    internal sealed class UserStatement : IStatementImpl
    {
        private readonly StatementSyntax _syntax;

        public StatementSyntax GetSyntax( TemplateSyntaxFactoryImpl? templateSyntaxFactory ) => this._syntax;

        public UserStatement( StatementSyntax syntax )
        {
            this._syntax = syntax;
        }
    }
}