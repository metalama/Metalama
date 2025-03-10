// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    internal sealed class ArrayUserExpression : UserExpression
    {
        private readonly ArrayBuilder _arrayBuilder;
        private readonly IType _itemType;

        public ArrayUserExpression( ArrayBuilder arrayBuilder )
        {
            this._arrayBuilder = arrayBuilder;

            this._itemType = this._arrayBuilder.ItemType;
            this.Type = this._itemType.MakeArrayType();
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        {
            var items = this._arrayBuilder.Items.SelectAsImmutableArray( i => TypedExpressionSyntaxImpl.FromValue( i, syntaxSerializationContext ).Syntax );

            var generator = syntaxSerializationContext.SyntaxGenerator;

            return generator.ArrayCreationExpression( generator.TypeSyntax( this._itemType ), items );
        }

        protected override bool CanBeNull => false;

        public override IType Type { get; }
    }
}