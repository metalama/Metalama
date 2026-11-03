// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue683_Switch;

// The override is the last statement of the template, so the return statements of the inlined void body are preserved.
// A return statement that is nested in a switch section must not be removed, because the end of a switch section must not be reachable.

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
    public void BracedSection( int i )
    {
        if (i < 0)
        {
            return;
        }

        switch (i)
        {
            case 0:
                {
                    Console.WriteLine( "zero" );

                    return;
                }

            default:
                break;
        }
    }

    [LogAspect]
    public void IfElseInSection( int i, bool b )
    {
        if (i < 0)
        {
            return;
        }

        switch (i)
        {
            case 0:
                if (b)
                {
                    return;
                }
                else
                {
                    return;
                }

            default:
                break;
        }
    }

    [LogAspect]
    public void NestedSwitch( int i, int j )
    {
        if (i < 0)
        {
            return;
        }

        switch (i)
        {
            case 0:
                switch (j)
                {
                    case 0:
                        return;

                    default:
                        return;
                }

            default:
                break;
        }
    }

    [LogAspect]
    public void DirectlyInSection( int i )
    {
        if (i < 0)
        {
            return;
        }

        switch (i)
        {
            case 0:
                Console.WriteLine( "zero" );

                return;

            default:
                break;
        }
    }
}
