// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Engine.Utilities.UserCode;

internal sealed class ExpressionHelper : IExpressionHelper
{
    private readonly SyntaxGenerationContext _syntaxGenerationContext;

    public ExpressionHelper( SyntaxGenerationContext syntaxGenerationContext )
    {
        this._syntaxGenerationContext = syntaxGenerationContext;
    }

    public string ConvertExpressionToText( IExpression expression )
        => expression switch
        {
            TypedConstant typedConstant => this._syntaxGenerationContext.SyntaxGenerator.TypedConstant( typedConstant ).ToString(),
            IContextlessExpression contextlessExpression => contextlessExpression.ToSyntax().ToString(),
            _ => throw new NotSupportedException( $"Cannot convert '{expression}' to C# syntax." )
        };
}