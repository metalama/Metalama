// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.UseEnumerableTemplateForAnyEnumerable;

public class AspectAttribute : OverrideMethodAspect
{
    public AspectAttribute()
    {
        UseEnumerableTemplateForAnyEnumerable = true;
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "default" );

        return meta.Proceed();
    }

    public override IEnumerable<dynamic?> OverrideEnumerableMethod()
    {
        Console.WriteLine( "enumerable" );

        return meta.ProceedEnumerable();
    }
}

// <target>
internal class EmptyOverrideFieldOrPropertyExample
{
    [Aspect]
    private IEnumerable<int> M()
    {
        return new[] { 42 };
    }
}