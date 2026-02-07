// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MethodCallReturningRunTimeValue;

// LEGITIMATE: A compile-time method call that returns a run-time value (e.g., IMethod.Invoke()).
// These are bridges to run-time code and do not have compile-time side effects.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            // meta.Target.Method.Invoke() is a compile-time member that returns a run-time value.
            // This is allowed because it generates run-time code, not compile-time side effects.
            return meta.Target.Method.Invoke( x );
        }

        return default;
    }
}

// <target>
internal class Target
{
    [Aspect]
    private int M( string? s ) => 0;
}
