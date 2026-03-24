// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if !NET472

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class RangeSerializer : ObjectSerializer<Range>
{
    public override ExpressionSyntax Serialize( Range obj, SyntaxSerializationContext serializationContext )
        => RangeExpression(
                IndexSerializer.SerializeIndex( obj.Start, serializationContext ),
                IndexSerializer.SerializeIndex( obj.End, serializationContext ) )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

    public RangeSerializer( SyntaxSerializationService service ) : base( service ) { }
}

#endif
