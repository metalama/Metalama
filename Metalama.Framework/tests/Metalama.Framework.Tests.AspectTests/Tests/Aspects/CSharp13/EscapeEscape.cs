#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.EscapeEscape;

// C# 13 feature: Escape sequence \e for the escape character

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("\e[1mThis is bold text from template.\e[0m");

        return meta.Proceed();
    }
}

// <target>
class Target
{
    [TheAspect]
    void M()
    {
        Console.WriteLine("\e[3mThis is italic text from target.\e[0m");
    }
}

#endif