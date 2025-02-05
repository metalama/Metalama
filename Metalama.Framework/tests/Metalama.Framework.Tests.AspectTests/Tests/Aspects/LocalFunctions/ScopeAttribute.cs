using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.ScopeAttribute;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CT();

        RT();

        return default;

        [CompileTime]
        void CT()
        {
        }

        [RunTime]
        void RT()
        {
        }
    }
}

class TargetCode
{
    // <target>
    [Aspect]
    void Method()
    {
    }
}