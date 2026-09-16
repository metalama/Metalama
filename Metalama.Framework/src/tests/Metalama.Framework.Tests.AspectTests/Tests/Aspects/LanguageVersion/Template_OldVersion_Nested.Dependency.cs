// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_OldVersion_Nested;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // The raw string literal is an operand of a binary expression. The verifier must descend into the operands
        // of the binary expression to detect that the template requires C# 11.
        Console.WriteLine( """raw""" + "suffix" );

        return meta.Proceed();
    }
}
