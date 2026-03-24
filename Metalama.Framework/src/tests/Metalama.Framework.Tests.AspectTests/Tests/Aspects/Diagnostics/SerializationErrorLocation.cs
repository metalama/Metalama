// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.SerializationErrorLocation;

internal class LogAttribute : OverrideMethodAspect
{
    [Introduce]
    private static string GetParametersDescription( IEnumerable<string> parameters ) => string.Join( "; ", parameters );

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( GetParametersDescription( meta.Target.Parameters.Select( p => p.Name ) ) );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Log]
    private void M() { }
}
