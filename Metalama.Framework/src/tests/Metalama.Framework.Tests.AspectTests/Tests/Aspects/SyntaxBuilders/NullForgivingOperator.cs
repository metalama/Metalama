// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.SyntaxBuilders.NullForgivingOperator;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertComment( "Test with nullable expression - should add !" ); 
        var nullableString = (object?) ExpressionFactory.Parse( "GetNullableString()" ).WithNullability( true ).WithNullForgivingOperator().Value;

        meta.InsertComment( "Test with null literal - should add !" );
        var nullLiteral = (object?) ExpressionFactory.Null().WithNullForgivingOperator().Value;

        meta.InsertComment( "Test with default expression - should add !");
        var defaultExpr = (object?) ExpressionFactory.Default<string>().WithNullForgivingOperator().Value;

        meta.InsertComment( "Test with force=true on non-nullable - should add !" );
        var forced = (object?) ExpressionFactory.Literal( "hello" ).WithNullForgivingOperator( force: true ).Value;
        
        meta.InsertComment( "Test with non-nullable - should NOT add !" );
        var nonNullable = (object?) ExpressionFactory.Literal( "hello" ).WithNullForgivingOperator( ).Value;

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [Aspect]
    private int M() => 0;

    private static string? GetNullableString() => null;
}
