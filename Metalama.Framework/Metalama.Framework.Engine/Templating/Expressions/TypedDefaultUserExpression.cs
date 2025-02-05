// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    internal sealed class TypedDefaultUserExpression : UserExpression
    {
        private readonly IType _givenType;

        public TypedDefaultUserExpression( IType type )
        {
            if ( type.IsReferenceType == true )
            {
                this.Type = type.ToNullable();
                this._givenType = type.ToNonNullable();
            }
            else
            {
                this.Type = this._givenType = type;
            }
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => syntaxSerializationContext.SyntaxGenerator.DefaultExpression( this._givenType, targetType );

        public override IType Type { get; }
    }
}