// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    // We add a cast to the original target type because the original expression may be target-typed, but may be used in a more weakly typed target.
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
        switch ( expression )
        {
#pragma warning disable RS1034
            case PostfixUnaryExpressionSyntax postFix when postFix.OperatorToken.Kind() == SyntaxKind.ExclamationToken:
#pragma warning restore RS1034
                return this.GetTypeConstant( postFix.Operand );

            case LiteralExpressionSyntax literal:
                var value = literal.Token.Value;

                if ( value != null )
                {
                    return TypedConstant.Create( value, this.Type.GetCompilationModel().Factory.GetTypeByReflectionType( value.GetType() ) );
                }
                else
                {
                    return TypedConstant.Default( this.Type );
                }

            case MemberAccessExpressionSyntax:
            case IdentifierNameSyntax:
                var semanticModel = this.Type.GetCompilationContext().SemanticModelProvider.GetSemanticModel( this.Expression.SyntaxTree );
                var member = semanticModel.GetSymbolInfo( expression ).Symbol;

                if ( member is IFieldSymbol field && field.ContainingType.TypeKind == TypeKind.Enum )
                {
                    var enumType = this.Type.GetCompilationModel().Factory.GetTypeByReflectionName( field.ContainingType.GetReflectionFullName() );

                    return TypedConstant.Create( field.ConstantValue, enumType );
                }
                else
                {
                    return null;
                }

            default:
                return null;
        }
    }

    protected override string ToStringCore() => this.AsString;
}