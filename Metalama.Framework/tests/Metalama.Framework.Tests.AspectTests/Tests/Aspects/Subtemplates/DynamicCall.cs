// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCall;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        meta.InvokeTemplate( nameof(CalledTemplateSimple) );
        meta.InvokeTemplate( new TemplateInvocation( nameof(CalledTemplateInvocation) ) );
        var templateProvider = TemplateProvider.FromInstance( new Templates() );
        meta.InvokeTemplate( nameof(Templates.CalledTemplateSimple), templateProvider );
        meta.InvokeTemplate( new TemplateInvocation( nameof(Templates.CalledTemplateInvocation), templateProvider ) );

        return default;
    }

    [Template]
    private void CalledTemplateSimple()
    {
        Console.WriteLine( "called template simple aspect" );
    }

    [Template]
    private void CalledTemplateInvocation()
    {
        Console.WriteLine( "called template invocation aspect" );
    }
}

[CompileTime]
internal class Templates : ITemplateProvider
{
    [Template]
    public void CalledTemplateSimple()
    {
        Console.WriteLine( "called template simple provider" );
    }

    [Template]
    public void CalledTemplateInvocation()
    {
        Console.WriteLine( "called template invocation provider" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}