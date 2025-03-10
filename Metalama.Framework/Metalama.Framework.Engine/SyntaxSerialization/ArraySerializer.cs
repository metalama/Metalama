// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class ArraySerializer : ObjectSerializer
{
    public ArraySerializer( SyntaxSerializationService serializers ) : base( serializers ) { }

    public override ExpressionSyntax Serialize( object obj, SyntaxSerializationContext serializationContext )
    {
        var array = (Array) obj;

        var elementType = array.GetType().GetElementType()!;

        if ( array.Rank > 1 )
        {
            throw SerializationDiagnosticDescriptors.MultidimensionalArray.CreateException( array.GetType() );
        }

        var lt = new List<ExpressionSyntax>();

        foreach ( var o in array )
        {
            lt.Add( this.Service.Serialize( o, serializationContext ) );
        }

        return ArrayCreationExpression(
            ArrayType(
                serializationContext.GetTypeSyntax( elementType ),
                SingletonList( ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( OmittedArraySizeExpression() ) ) ) ),
            InitializerExpression( SyntaxKind.ArrayInitializerExpression, SeparatedList( lt ) ) );
    }

    public override Type InputType => typeof(Array);

    public override Type OutputType => throw new NotSupportedException();
}