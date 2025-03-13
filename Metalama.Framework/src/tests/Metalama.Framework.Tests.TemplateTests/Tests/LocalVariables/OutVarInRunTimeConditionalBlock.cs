// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.OutVarInRunTimeConditionalBlock;

[CompileTime]
internal class Aspect
{
    private void M( out int i, out int j ) => ( i, j ) = ( 1, 2 );

    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.Target.Parameters.Single().Value > 0)
        {
            var i = meta.CompileTime( 0 );
            M( out i, out var j );
            j++;
            Console.WriteLine( $"i={i} j={j}" );
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