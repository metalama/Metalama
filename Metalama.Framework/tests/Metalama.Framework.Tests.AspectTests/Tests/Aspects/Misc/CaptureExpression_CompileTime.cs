// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CaptureExpression_CompileTime;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        foreach (var parameter in meta.Target.Parameters)
        {
            if (parameter.Value == null)
            {
                throw GetNewExceptionExpression( parameter.Name, $"Parameter {parameter} can't be null." ).Value!;
            }
        }

        return meta.Proceed();
    }

    [CompileTime]
    private static IExpression GetNewExceptionExpression( string parameterName, string errorMessage )
        => ExpressionFactory.Capture( new ArgumentNullException( parameterName, errorMessage ) );
}

// <target>
internal class TargetClass
{
    [TestAspect]
    private void M( object obj ) { }
}