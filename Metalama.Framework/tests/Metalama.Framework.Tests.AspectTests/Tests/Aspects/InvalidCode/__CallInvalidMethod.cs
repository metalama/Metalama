using System;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.CallInvalidMethod
{
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            InvalidMethod();
            return default;
        }
    }

    class TargetCode
    {
        [Aspect]
        int Method(int a)
        {
            return a;
        }
    }
}