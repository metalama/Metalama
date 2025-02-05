// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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