// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.InvalidSyntaxExpressionBuilder;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var eb = new ExpressionBuilder();

        if (meta.Target.Parameters.Count > 0)
        {
            var i = meta.CompileTime( 0 );

            foreach (var parameter in meta.Target.Parameters)
            {
                if (i > 0)
                {
                    eb.AppendVerbatim( ", " );
                }

                eb.AppendExpression( parameter );
                i++;
            }

            Console.WriteLine( eb.ToValue() );
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private int Method( int a, int b )
    {
        return a;
    }
}