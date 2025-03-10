// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DirectCall;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        CalledTemplateInstance();
        CalledTemplateStatic();
        new AnotherClass().CalledTemplateInstance();
        AnotherClass.CalledTemplateStatic();

        return default;
    }

    [Template]
    private void CalledTemplateInstance()
    {
        Console.WriteLine( "called template instance aspect" );
    }

    [Template]
    private static void CalledTemplateStatic()
    {
        Console.WriteLine( "called template static aspect" );
    }
}

internal class AnotherClass : ITemplateProvider
{
    [Template]
    public void CalledTemplateInstance()
    {
        Console.WriteLine( "called template instance another" );
    }

    [Template]
    public static void CalledTemplateStatic()
    {
        Console.WriteLine( "called template static another" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}