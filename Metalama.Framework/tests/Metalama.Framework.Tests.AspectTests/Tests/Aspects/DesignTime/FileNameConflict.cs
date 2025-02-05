#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.FileNameConflict
{
    // Tests that the pipeline handles types with the same full name.

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void Foo() { }
    }

    namespace X
    {
        [Introduction]
        partial class Y
        {
        }

        [Introduction]
        partial class Y<T>
        {
        }
    }

    partial class X<T>
    {
        [Introduction]
        partial class Y
        {
        }

        [Introduction]
        partial class Y<U>
        {
        }
    }
}