// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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