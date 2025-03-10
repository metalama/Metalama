// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SourceReference;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( Path.GetFileName( meta.Target.Declaration.Sources.Single( s => s.IsImplementationPart ).Span.FilePath ) ) );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M() { }
}