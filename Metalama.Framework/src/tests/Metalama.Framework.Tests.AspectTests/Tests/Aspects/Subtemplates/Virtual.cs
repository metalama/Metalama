// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "normal template" );

        switch (meta.Target.Parameters["x"].Value)
        {
            case 0:
                CalledTemplate();

                break;

            case 1:
                CalledTemplate();

                break;

            case 2:
            {
                var aspect = this;
                aspect.CalledTemplate();
            }

                break;
        }

        throw new Exception();
    }

    [Template]
    protected virtual void CalledTemplate()
    {
        Console.WriteLine( "base called template" );
    }
}

internal class DerivedAspect : Aspect
{
    protected override void CalledTemplate()
    {
        Console.WriteLine( "derived called template" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method1( int x ) { }

    [DerivedAspect]
    private void Method2( int x ) { }
}