using System;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.CallMethodWithInvalidSignatureInOtherType
{
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var x = OtherType.Method();
            return default;
        }
    }

    [CompileTime]
    class OtherType
    {
        public static BadType? Method() => null;
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