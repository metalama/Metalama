using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.AnonymousMethod_Neutral;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        // Not supported because neutral.
        var action = delegate() { return "Hello, world."; };

        return null;
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}