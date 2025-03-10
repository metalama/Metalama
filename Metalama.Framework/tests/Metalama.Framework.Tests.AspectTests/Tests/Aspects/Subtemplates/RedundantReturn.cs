// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.RedundantReturn;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate();
        CalledTemplateIf( false );
        CalledTemplateSwitch( 0 );

        return default;
    }

    [Template]
    private void CalledTemplate()
    {
        Console.WriteLine( "No condition." );

        return;
    }

    [Template]
    private void CalledTemplateIf( [CompileTime] bool condition )
    {
        if (condition)
        {
            Console.WriteLine( "Condition is true." );

            return;
        }
        else
        {
            Console.WriteLine( "Condition is false." );

            return;
        }
    }

    [Template]
    private void CalledTemplateSwitch( [CompileTime] int i )
    {
        switch (i)
        {
            case 0:
            case 1:
                Console.WriteLine( "1 or 2" );

                return;

            case 3:
                Console.WriteLine( "3" );

                return;

            case 4:
                Console.WriteLine( "5" );

                throw new Exception();

            default:
                return;
        }
    }

    [Template]
    private void CalledTemplateTry()
    {
        try
        {
            Console.WriteLine( "try" );

            return;
        }
        catch (Exception)
        {
            Console.WriteLine( "catch" );

            return;
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}