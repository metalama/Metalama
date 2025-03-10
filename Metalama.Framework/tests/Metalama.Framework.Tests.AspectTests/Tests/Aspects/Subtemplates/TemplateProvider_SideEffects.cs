// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateProvider_SideEffects;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        new Templates().CalledTemplate();
        new Templates().CalledTemplate();

        return meta.Proceed();
    }
}

internal class Templates : ITemplateProvider
{
    private static int i;

    public Templates()
    {
        i++;
    }

    [Template]
    public void CalledTemplate()
    {
        Console.WriteLine( $"called template i={i}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}