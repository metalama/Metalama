// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// An implementation of <see cref="UserExpression"/> where the syntax is known upfront.
    /// </summary>
    internal class SyntaxUserExpression : UserExpression
    {
        public SyntaxUserExpression(
            ExpressionSyntax expression,
            IType type,
            bool? isReferenceable = null,
            bool? isAssignable = null )
        {
            Invariant.Assert( type is not { TypeKind: TypeKind.Dynamic } );

            this.Expression = expression;
            this.Type = type;
            this.IsAssignable = isAssignable;
            this.IsReferenceable = isReferenceable;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null ) => this.Expression;

        public override IType Type { get; }

        protected override bool? IsAssignable { get; }

        private protected override bool? IsReferenceable { get; }

        protected ExpressionSyntax Expression { get; }

        protected override string ToStringCore() => this.Expression.ToString();
    }
}