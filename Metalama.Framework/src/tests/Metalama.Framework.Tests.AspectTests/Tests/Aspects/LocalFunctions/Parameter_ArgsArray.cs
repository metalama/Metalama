// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Parameter_ArgsArray;

internal class Aspect : MethodAspect
{
    [Template]
    public Func<object?, object?[], object?> GetMethodInvoker( IMethod method )
    {
        return Invoke;

        object? Invoke( object? instance, object?[] args )
        {
            return method.With( instance ).Invoke( args[0]! );
        }
    }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.With( builder.Target.DeclaringType )
            .IntroduceMethod(
                nameof(GetMethodInvoker),
                args: new { method = builder.Target } );
    }
}

// <target>
internal class C
{
    [Aspect]
    private int M( int i ) => 42;
}