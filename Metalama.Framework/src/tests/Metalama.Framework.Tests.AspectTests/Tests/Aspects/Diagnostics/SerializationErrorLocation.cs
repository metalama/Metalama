// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.SerializationErrorLocation;

internal class LogAttribute : OverrideMethodAspect
{
    [Introduce]
    private static string GetDescription( object value ) => value.ToString()!;

    public override dynamic? OverrideMethod()
    {
        // meta.Target.Parameters is a compile-time IParameterList. Passing it to an introduced method
        // requires serialization, which fails because IParameterList is not serializable.
        // Using meta.RunTime forces the compile-time value to be serialized.
        object method = meta.Target.Method;
        Console.WriteLine( GetDescription( meta.RunTime( method ) ) );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Log]
    private void M() { }
}
