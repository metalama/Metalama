// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;

#pragma warning disable CS0169

// The condition of the `if` statement combines a compile-time operand and a run-time operand, which reports LAMA0241.
// The classification of the template must not throw after this error (#2156).

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.IfConditionWithScopeConflict;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var logger = meta.Target.Type.FieldsAndProperties.FirstOrDefault( f => f.Name == "_logger" );

        if (logger != null && logger.Value != null)
        {
            Console.WriteLine( "The logger is not null." );
        }
        else if (logger != null && logger.Value == null)
        {
            Console.WriteLine( "The logger is null." );
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private object? _logger;

    [Aspect]
    private void Method() { }
}
