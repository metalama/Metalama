using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.TargetTypedInitializationExpression;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void IntroducedMethod()
    {
        var initializerExpression = meta.Target.Type.Fields.Single().InitializerExpression;
        _ = new StrongBox<object>( initializerExpression.Value );
    }
}

// <target>
[TheAspect]
public class C
{
    public List<int> List = [1, 2, 3];
}