// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.SyntaxBuilders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating.Statements;

internal interface IStatementListImpl : IStatementList
{
    public IReadOnlyList<StatementSyntax> GetSyntaxes( TemplateSyntaxFactoryImpl? templateSyntaxFactory );
}