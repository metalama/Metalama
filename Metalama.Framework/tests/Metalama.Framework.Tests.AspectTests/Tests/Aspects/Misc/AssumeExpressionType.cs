using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.AssumeExpressionType;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void SomeMethod( object nonNullable, object? nullable )
    {
        var nullableExpression = ExpressionFactory.Capture( nullable );
        var nonNullableExpression = ExpressionFactory.Capture( nonNullable );

        meta.InsertComment( "With original nullability" );
        _ = nullableExpression.Value!.ToString();
        _ = nonNullableExpression.Value!.ToString();

        meta.InsertComment( "With inverse nullability" );
        _ = nullableExpression.WithNullability( false ).Value!.ToString();
        _ = nonNullableExpression.WithNullability( true ).Value!.ToString();
    }
}

// <target>
[MyAspect]
internal class C { }