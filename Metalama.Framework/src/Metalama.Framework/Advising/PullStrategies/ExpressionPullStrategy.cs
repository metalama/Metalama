// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising.PullStrategies;

internal sealed class ExpressionPullStrategy : IPullStrategy
{
    private readonly IExpression _expression;

    public ExpressionPullStrategy( IExpression expression )
    {
        this._expression = expression;
    }

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember ) => PullAction.UseExpression( this._expression );

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<ExpressionPullStrategy>
    {
        public override ExpressionPullStrategy CreateInstance( IArgumentsReader constructorArguments )
        {
            var expressionText = constructorArguments.GetValue<string>( "expression" )!;

            return new ExpressionPullStrategy( ExpressionFactory.Parse( expressionText ) );
        }

        public override void SerializeObject( ExpressionPullStrategy obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( "expression", obj._expression.ToText() );
        }
    }
}