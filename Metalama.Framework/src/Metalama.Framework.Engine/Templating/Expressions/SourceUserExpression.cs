// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypedConstant = Metalama.Framework.Code.TypedConstant;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class SourceUserExpression : SyntaxUserExpression, ISourceExpression
{
    public SourceUserExpression( ExpressionSyntax expression, IType type, bool isReferenceable = false, bool isAssignable = false ) : base(
        expression,
        type,
        isReferenceable,
        isAssignable ) { }

    public object AsSyntaxNode => this.Expression;

    // When targetType differs from this.Type, we add a cast to this.Type (not to targetType) because the original
    // source expression may be target-typed in its original context. The cast ensures the expression retains its
    // semantics when placed in a different context. The output type is always this.Type, so no GetSyntaxType override is needed.
    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        if ( targetType?.Equals( this.Type ) != true )
        {
            return syntaxSerializationContext.SyntaxGenerator.CastExpression( this.Type, this.Expression );
        }
        else
        {
            return this.Expression;
        }
    }

    [Memo]
    public string AsString => this.Expression.NormalizeWhitespace().ToString();

    [Memo]
    public string AsFullString => this.Expression.NormalizeWhitespace().ToFullString();

    [Memo]
    public TypedConstant? AsTypedConstant => this.GetTypeConstant( this.Expression );

    private TypedConstant? GetTypeConstant( ExpressionSyntax expression )
    {
        var expressionKind = expression.Kind();

#pragma warning disable RS1034
        if ( expressionKind == SyntaxKind.SuppressNullableWarningExpression
             && expression is PostfixUnaryExpressionSyntax postFix
             && postFix.OperatorToken.Kind() == SyntaxKind.ExclamationToken )
        {
            return this.GetTypeConstant( postFix.Operand );
        }
#pragma warning restore RS1034

        if ( expressionKind.IsLiteralExpression )
        {
            var literal = (LiteralExpressionSyntax) expression;
            var value = literal.Token.Value;

            if ( value != null )
            {
                return TypedConstant.Create( value, this.Type.GetCompilationModel().Factory.GetTypeByReflectionType( value.GetType() ) );
            }
            else
            {
                return TypedConstant.Default( this.Type );
            }
        }
        else if ( expressionKind is SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.IdentifierName )
        {
            var semanticModel = this.Type.GetCompilationContext().SemanticModelProvider.GetSemanticModel( this.Expression.SyntaxTree );
            var member = semanticModel.GetSymbolInfo( expression ).Symbol;

            if ( member?.Kind == SymbolKind.Field && member is IFieldSymbol field && field.ContainingType.TypeKind == TypeKind.Enum )
            {
                var enumType = this.Type.GetCompilationModel().Factory.GetTypeByReflectionName( field.ContainingType.GetReflectionFullName() );

                return TypedConstant.Create( field.ConstantValue, enumType );
            }
        }

        return null;
    }

    protected override string ToStringCore() => this.AsString;
}