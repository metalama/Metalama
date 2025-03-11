// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.Argument_TypedConstant;

public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        if (meta.Target.Parameters[0].Value == 0)
        {
            var expr = ExpressionFactory.Capture( TypedConstant.Create( 42 ) );

            return meta.Target.Method.Invoke( TypedConstant.Create( 42 ), expr );
        }
        else
        {
            return meta.Proceed();
        }
    }
}

// <target>
internal class TargetClass
{
    [Test]
    private void M( int i, int j ) => Console.WriteLine( i + j );
}