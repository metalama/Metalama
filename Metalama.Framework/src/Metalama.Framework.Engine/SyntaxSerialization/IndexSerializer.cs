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
        => ObjectCreationExpression(
                serializationContext.GetTypeSyntax( typeof(Index) ),
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal( obj.Value ) ) ),
                            Argument(
                                obj.IsFromEnd
                                    ? LiteralExpression( SyntaxKind.TrueLiteralExpression )
                                    : LiteralExpression( SyntaxKind.FalseLiteralExpression ) )
                        } ) ),
                null )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

    public IndexSerializer( SyntaxSerializationService service ) : base( service ) { }
}

#endif
