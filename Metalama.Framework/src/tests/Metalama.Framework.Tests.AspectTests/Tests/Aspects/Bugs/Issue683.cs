// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue683;

// The override is the last statement of the template, so the return statements of the inlined void body
// must stay return statements instead of being replaced by goto statements.

internal class LogAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "logged" );

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [LogAspect]
    public void Method( int i )
    {
        if (i == 0)
        {
            return;
        }

        Console.WriteLine( "method" );
    }
}
