// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCallWithArgs_Durable;

/// <summary>
/// Verifies that a durable expression passed by <c>meta.InvokeTemplate</c> is resolved before it is bound to a run-time parameter
/// or to a compile-time parameter of the called template.
/// </summary>
internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate(
            nameof(CalledTemplate),
            args: new { a = ExpressionFactory.Parse( "DateTime.Now.Year" ).ToDurable(), b = ExpressionFactory.Parse( "42 + 1" ).ToDurable() } );

        return default;
    }

    [Template]
    private void CalledTemplate( int a, [CompileTime] IExpression b )
    {
        Console.WriteLine( $"called template a={a}, b={b.Value}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}
