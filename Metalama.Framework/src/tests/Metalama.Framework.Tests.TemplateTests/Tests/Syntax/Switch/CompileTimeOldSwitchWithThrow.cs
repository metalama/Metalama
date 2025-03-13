// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.CompileTimeOldSwitchWithThrow;

[CompileTime]
internal enum SwitchEnum
{
    one = 1,
    two = 2
}

internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var i = SwitchEnum.one;

        switch (i)
        {
            case SwitchEnum.one:
            case SwitchEnum.two:
                Console.WriteLine( "1 or 2" );

                break;

            default:
                throw new Exception();
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}