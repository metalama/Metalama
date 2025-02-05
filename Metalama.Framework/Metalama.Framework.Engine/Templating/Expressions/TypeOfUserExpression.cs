// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    internal sealed class TypeOfUserExpression : UserExpression
    {
        private readonly IType _type;

        public TypeOfUserExpression( IType type )
        {
            this._type = type;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => syntaxSerializationContext.SyntaxGenerator.TypeOfExpression( this._type );

        protected override bool CanBeNull => false;

        public override IType Type => this._type.Compilation.Factory.GetTypeByReflectionType( typeof(Type) );
    }
}