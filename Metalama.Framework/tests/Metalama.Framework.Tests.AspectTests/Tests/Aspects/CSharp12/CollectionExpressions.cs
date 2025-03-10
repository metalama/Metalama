// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.CollectionExpressions;

public class TheAspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        int[] collection = [1, 2, 3, ..Enumerable.Range( 3, 2 )];
    }

    public override dynamic? OverrideMethod()
    {
        int[] collection1 = [1, 2, 3, ..Enumerable.Range( 3, 2 )];
        Console.WriteLine( collection1 );

        int[] collection2 = [meta.Target.Parameters[0].Value, 2, 3, .. Enumerable.Range( 3, 2 )];
        Console.WriteLine( collection2 );

        var collection3 = meta.CompileTime<int[]>( [1, 2, 3, .. Enumerable.Range( 3, 2 )] );
        Console.WriteLine( collection3 );

        int[] collection4 = [1, meta.CompileTime( 2 ), 3, ..Enumerable.Range( 3, 2 )];
        Console.WriteLine( collection4 );

        int[] collection5 = [1, 2, 3, ..meta.CompileTime( Enumerable.Range( 3, 2 ) )];
        Console.WriteLine( collection5 );

        return meta.Proceed();
    }
}

public class C
{
    // <target>
    [TheAspect]
    private static void M( int i )
    {
        int[] collection = [1, 2, ..Enumerable.Range( 3, 2 )];
    }
}

#endif