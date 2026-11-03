// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Issue2235.Options;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Issue2235.Tests;

public class PrintLabelAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.Target.Method.Enhancements().GetOptions<LabelOptions>().Label );

        return meta.Proceed();
    }
}

// <target>
[Label( "from the type" )]
internal class Target
{
    [PrintLabel]
    public void M() { }
}
