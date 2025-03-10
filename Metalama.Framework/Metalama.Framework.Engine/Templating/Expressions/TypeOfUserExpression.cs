// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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