#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using System;
using System.Text;
using Metalama.Testing.AspectTesting;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.New.NewInvalidType
{
    class Aspect
    {
        [TestTemplate]
        dynamic? Template()
        {
            var o = new NonExistingType();
            
            return meta.Proceed();
        }
    }

    // <target>
    class TargetCode
    {
        int Method(int a)
        {
            return a;
        }
    }
}