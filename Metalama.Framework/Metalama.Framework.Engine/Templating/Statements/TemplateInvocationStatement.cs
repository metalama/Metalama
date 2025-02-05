// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating.Statements;

internal sealed class TemplateInvocationStatement : IStatementImpl
{
    private readonly TemplateInvocation _templateInvocation;
    private readonly object? _args;

    public TemplateInvocationStatement( TemplateInvocation templateInvocation, object? args )
    {
        this._templateInvocation = templateInvocation;
        this._args = args;
    }

    public StatementSyntax GetSyntax( TemplateSyntaxFactoryImpl? templateSyntaxFactory )
    {
        if ( templateSyntaxFactory == null )
        {
            throw new InvalidOperationException( "Template invocation is not available in the current context." );
        }

        return templateSyntaxFactory.InvokeTemplate( this._templateInvocation, this._args );
    }
}