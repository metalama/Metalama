// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class TimeSpanSerializer : ObjectSerializer<TimeSpan>
{
    public override ExpressionSyntax Serialize( TimeSpan obj, SyntaxSerializationContext serializationContext )
        => ObjectCreationExpression(
                serializationContext.GetTypeSyntax( typeof(TimeSpan) ),
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal( obj.Ticks ) ) ) ) ),
                null )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

    public TimeSpanSerializer( SyntaxSerializationService service ) : base( service ) { }
}