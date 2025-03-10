// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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