// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if !NET472
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class IndexSerializer : ObjectSerializer<Index>
{
    public override ExpressionSyntax Serialize( Index obj, SyntaxSerializationContext serializationContext )
        => SerializeIndex( obj, serializationContext )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

    /// <summary>
    /// Serializes an <see cref="Index"/> to its C# expression form without whitespace normalization.
    /// Used by <see cref="RangeSerializer"/> to compose range expressions.
    /// </summary>
    internal static ExpressionSyntax SerializeIndex( Index obj, SyntaxSerializationContext serializationContext )
    {
        var valueLiteral = LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( obj.Value ) );

        if ( obj.IsFromEnd )
        {
            // Generate ^n syntax.
            return PrefixUnaryExpression( SyntaxKind.IndexExpression, valueLiteral );
        }
        else
        {
            // Generate Index.FromStart(n) to ensure the expression type is System.Index.
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serializationContext.GetTypeSyntax( typeof(Index) ),
                    IdentifierName( "FromStart" ) ),
                ArgumentList( SingletonSeparatedList( Argument( valueLiteral ) ) ) );
        }
    }

    public IndexSerializer( SyntaxSerializationService service ) : base( service ) { }
}

#endif