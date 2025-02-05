using System.Linq.Expressions;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.NestedRunTimeOrCompileTimeInvocations;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );
        var p = Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );

        return Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private Expression? M() => null;
}