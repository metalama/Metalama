// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal class CreateTupleExpression : UserExpression
{
    private readonly ITupleType _tupleType;
    private readonly Func<SyntaxSerializationContext, IReadOnlyList<TypedExpressionSyntaxImpl>> _getValues;

    public CreateTupleExpression( ITupleType tupleType, Func<SyntaxSerializationContext, IReadOnlyList<TypedExpressionSyntaxImpl>> getValues )
    {
        this._tupleType = tupleType;
        this._getValues = getValues;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var values = this._getValues( syntaxSerializationContext )
            .Select(
                ( v, i ) =>
                    SyntaxFactory.Argument(
                        v
                            .Convert( this._tupleType.TupleElements[i].Type, syntaxSerializationContext.SyntaxGenerationContext, true )
                            .Syntax ) )
            .ToReadOnlyList();

        return syntaxSerializationContext.SyntaxGenerator.TupleExpression( this._tupleType, values, this.RequiresQualifiedArguments( targetType ) );
    }

    public override IType Type => this._tupleType;

    private bool RequiresQualifiedArguments( IType? targetType )
    {
        if ( targetType is not ITupleType targetTupleType )
        {
            return false;
        }

        if ( this._tupleType.TupleLength != targetTupleType.TupleLength )
        {
            return false;
        }

        for ( var i = 0; i < this._tupleType.TupleElements.Count; i++ )
        {
            if ( this._tupleType.TupleElements[i].Name == targetTupleType.TupleElements[i].Name )
            {
                return false;
            }
        }

        return true;
    }
}