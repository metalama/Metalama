// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal class TupleItemExpression : UserExpression
{
    private readonly ITupleType _tupleType;
    private readonly IUserExpression _instance;
    private readonly int _index;
    private readonly InvokerOptions _options;

    public TupleItemExpression( ITupleType tupleType, IUserExpression instance, int index, InvokerOptions options )
    {
        this._tupleType = tupleType;
        this._instance = instance;
        this._index = index;
        this._options = options;

        var orderOptions = options & InvokerOptions.OrderMask;

        if ( orderOptions != 0 )
        {
            throw new ArgumentOutOfRangeException( nameof(options), $"The flag '{orderOptions}' is forbidden." );
        }
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var instance = this._instance.ToTypedExpressionSyntax( syntaxSerializationContext );
        var item = SyntaxFactory.IdentifierName( this._tupleType.TupleElements[this._index].Name );

        return (instance.ExpressionType?.IsNullable, this._options) switch
        {
            // Use instance?.Item
            (true, InvokerOptions.NullConditionalIfNullable) or (_, InvokerOptions.NullConditional) =>
                SyntaxFactory.ConditionalAccessExpression( instance.Syntax, SyntaxFactory.MemberBindingExpression( item ) ),

            // Use instance!.Value.Item
            (true, InvokerOptions.SuppressNullableWarningIfNullable or InvokerOptions.SuppressNullableWarning)
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, instance.Syntax ),
                        SyntaxFactory.IdentifierName( "Value" ) ),
                    item ),

            // Use instance.Value.Item
            (true, _)
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance.Syntax,
                        SyntaxFactory.IdentifierName( "Value" ) ),
                    item ),

            // Default: use instance.Item
            _ => SyntaxFactory.MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, instance.Syntax, item )
        };
    }

    public override IType Type
    {
        get
        {
            var itemType = this._tupleType.TupleElements[this._index].Type;

            if ( itemType.IsNullable == true && this._options is (InvokerOptions.NullConditionalIfNullable or InvokerOptions.NullConditional) )
            {
                return itemType.ToNullable();
            }
            else
            {
                return itemType;
            }
        }
    }
}