// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateInStaticClass_NotProvider;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        StaticClass.StaticTemplate( 1, 2 );

        return default;
    }
}

[RunTimeOrCompileTime]
internal static class StaticClass
{
    [Template]
    public static void StaticTemplate( int i, [CompileTime] int j )
    {
        Console.WriteLine( $"static template i={i}, j={j}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}