// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class ExpressionBuilderSerializer : ObjectSerializer<IExpressionBuilder>
    {
        public ExpressionBuilderSerializer( SyntaxSerializationService service ) : base( service ) { }

        public override ExpressionSyntax Serialize( IExpressionBuilder obj, SyntaxSerializationContext serializationContext )
            => obj.ToExpression().ToExpressionSyntax( serializationContext );

        public override Type? OutputType => null;
    }
}