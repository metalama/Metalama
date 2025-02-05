using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Template;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Generate();

        Introduce();

        return default;

        [Template]
        void Generate()
        {
        }

        [Introduce]
        void Introduce()
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