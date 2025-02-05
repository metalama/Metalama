using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33329;

// <target>
[CompileTime]
internal class C
{
    [CompileTime]
    private void M()
    {
        LocalFunction();
        StaticLocalFunction();

        void LocalFunction() { }

        static void StaticLocalFunction() { }
    }
}