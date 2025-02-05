#if TEST_OPTIONS
// @KeepDisabledCode
#endif

using System.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30928
{
    public interface ISomeInterface
    {
    }

    // <target>
    public class SomeDisposable : ISomeInterface
    {   
#if TESTRUNNER
        void Foo()
        {
            Debug.Fail("");
        }
#endif

#if !TESTRUNNER
        void Bar()
        {
            Debug.Fail("");
        }
#endif
    }
}
