using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.FunctionPointerParameters
{
    internal class MyAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        internal unsafe void M( delegate*<string?, int?, string?[], int?[], (string?, int?), (string?[], int?[])> p ) { }
    }
}