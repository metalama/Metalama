// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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