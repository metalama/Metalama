using System;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.InvalidTypeInCompileTimeMethod
{
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            return default;
        }
        
        [Template]
        public void MyTemplate( X t ) {}
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