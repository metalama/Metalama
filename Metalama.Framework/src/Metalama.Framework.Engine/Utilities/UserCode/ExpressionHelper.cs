// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
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

    /// <remarks>
    /// Unlike <see cref="ConvertExpressionToText"/>, which serves only the two shapes that can render themselves
    /// without a context, this renders through <c>UserExpression.ToSyntax</c> and therefore accepts every shape.
    /// </remarks>
    public IDurableExpression ToDurableExpression( IExpression expression )
    {
        var compilation = (CompilationModel) expression.Type.Compilation;
        var context = new SyntaxSerializationContext( compilation, this._syntaxGenerationContext, null, null );

        switch ( expression )
        {
            case TypedConstant typedConstant:
                return new DurableExpression(
                    context.SyntaxGenerator.TypedConstant( typedConstant ),
                    typedConstant.Type,
                    false,
                    false );

            case UserExpression userExpression:
                {
                    // The type the expression will be used as is not known here, which is the one thing the eager
                    // rendering gives up. See the remarks on DurableExpression.
                    var typedSyntax = userExpression.ToTypedExpressionSyntax( context );

                    return new DurableExpression(
                        typedSyntax.Syntax,
                        typedSyntax.ExpressionType ?? expression.Type,
                        typedSyntax.IsReferenceable,
                        expression.IsAssignable );
                }

            default:
                throw new NotSupportedException( $"Cannot make '{expression}' durable." );
        }
    }
}