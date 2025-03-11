// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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