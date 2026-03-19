// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal abstract partial class Invoker<T>
{
    internal readonly record struct ReceiverTypedExpressionSyntax
    {
        private readonly TypedExpressionSyntaxImpl _typedExpressionSyntax;

        public ReceiverTypedExpressionSyntax(
            TypedExpressionSyntaxImpl typedExpressionSyntax,
            InvokerOptions options,
            AspectReferenceSpecification aspectReferenceSpecification )
        {
            (this.RequiresConditionalAccess, var requiresNullableWarningSuppression) =
                (options & InvokerOptions.NullabilityMask, typedExpressionSyntax.CanBeNull) switch
                {
                    (InvokerOptions.NullConditional, _) or (InvokerOptions.NullConditionalIfNullable, true) => (true, false),
                    (InvokerOptions.SuppressNullableWarning, _) or (InvokerOptions.SuppressNullableWarningIfNullable, true) => (false, true),
                    _ => (false, false)
                };

            if ( this.RequiresConditionalAccess && typedExpressionSyntax.Syntax.IsKind( SyntaxKind.SuppressNullableWarningExpression ) )
            {
                // Remove the ! token.
                this._typedExpressionSyntax = new TypedExpressionSyntaxImpl(
                    ((PostfixUnaryExpressionSyntax) typedExpressionSyntax.Syntax).Operand,
                    typedExpressionSyntax,
                    true );
            }
            else if ( requiresNullableWarningSuppression && !typedExpressionSyntax.Syntax.IsKind( SyntaxKind.SuppressNullableWarningExpression ) )
            {
                this._typedExpressionSyntax = new TypedExpressionSyntaxImpl(
                    SyntaxFactory.PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, typedExpressionSyntax.Syntax ),
                    typedExpressionSyntax,
                    false );
            }
            else
            {
                this._typedExpressionSyntax = typedExpressionSyntax;
            }

            this.AspectReferenceSpecification = aspectReferenceSpecification;
        }

        public ExpressionSyntax Syntax => this._typedExpressionSyntax.Syntax;

        public bool RequiresConditionalAccess { get; }

        public AspectReferenceSpecification AspectReferenceSpecification { get; }

        public ReceiverExpressionSyntax WithSyntax( ExpressionSyntax syntax )
            => new( syntax, this.RequiresConditionalAccess, this.AspectReferenceSpecification );

        public ReceiverExpressionSyntax ToReceiverExpressionSyntax() => new( this.Syntax, this.RequiresConditionalAccess, this.AspectReferenceSpecification );

        public ExpressionSyntax GetReceiverSyntax(
            IMember declaration,
            SyntaxSerializationContext context )
        {
            if ( declaration.IsStatic )
            {
                return context.SyntaxGenerator.TypeExpression( declaration.DeclaringType )
                    .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
            }

            var definition = declaration.Definition;

            if ( definition.IsExplicitInterfaceImplementation )
            {
                return
                    SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.CastExpression(
                                context.SyntaxGenerator.TypeSyntax( definition.GetExplicitInterfaceImplementation().DeclaringType ),
                                this._typedExpressionSyntax.Syntax ) )
                        .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
            }

            return this._typedExpressionSyntax.Convert( declaration.DeclaringType, context.SyntaxGenerationContext ).Syntax;
        }
    }
}