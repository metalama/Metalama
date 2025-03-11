// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Misc.NestedNestedAspect;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

[CompileTime]
public class Outer
{
    public class Inner
    {
        public class LogAttribute : OverrideMethodAspect
        {
            public override dynamic? OverrideMethod()
            {
                Console.WriteLine( meta.Target.Method.ToDisplayString() + " started." );

                return meta.Proceed();
            }
        }
    }
}

// <target>
internal class C
{
    [Outer.Inner.Log]
    private void M() { }
}