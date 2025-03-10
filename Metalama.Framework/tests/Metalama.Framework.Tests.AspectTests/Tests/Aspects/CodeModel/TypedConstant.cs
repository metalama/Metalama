// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.TypedConstant_;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var expressionBuilder = new ExpressionBuilder();

        expressionBuilder.AppendExpression( new MyExpressionBuilder() );
        expressionBuilder.AppendVerbatim( "+" );
        expressionBuilder.AppendExpression( GetExpression() );
        expressionBuilder.AppendVerbatim( "+" );
        Append( expressionBuilder );

        return expressionBuilder.ToValue();
    }

    private IExpression GetExpression() => TypedConstant.Create( 42 );

    private void Append( ExpressionBuilder expressionBuilder ) => expressionBuilder.AppendExpression( TypedConstant.Create( 42 ) );
}

[CompileTime]
internal class MyExpressionBuilder : IExpressionBuilder
{
    public IExpression ToExpression() => TypedConstant.Create( 42 );
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private int Method( int a )
    {
        return a;
    }
}