// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    internal sealed class CastUserExpression : UserExpression
    {
        private readonly object? _value;

        public CastUserExpression( IType type, object? value )
        {
            this.Type = type;
            this._value = value;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        {
            var valueSyntax = this._value switch
            {
                ExpressionSyntax e => e,
                TypedExpressionSyntaxImpl runtimeExpression => runtimeExpression.Syntax,
                TypedExpressionSyntax runtimeExpression => runtimeExpression.Syntax,
                IUserExpression ue => ue.ToExpressionSyntax( syntaxSerializationContext, targetType ),
                _ => throw new AssertionFailedException( $"Unexpected value type: '{this._value?.GetType()}'." )
            };

            return SyntaxFactory.ParenthesizedExpression( syntaxSerializationContext.SyntaxGenerator.CastExpression( this.Type, valueSyntax ) )
                .WithSimplifierAnnotationIfNecessary( syntaxSerializationContext.SyntaxGenerationContext );
        }

        public override IType Type { get; }
    }
}