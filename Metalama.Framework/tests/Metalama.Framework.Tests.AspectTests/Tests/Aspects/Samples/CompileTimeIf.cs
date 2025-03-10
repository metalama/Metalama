// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.CompileTimeIf
{
    internal class CompileTimeIfAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            if (meta.Target.Method.IsStatic)
            {
                Console.WriteLine( $"Invoking {meta.Target.Method.ToDisplayString()}" );
            }
            else
            {
                Console.WriteLine( $"Invoking {meta.Target.Method.ToDisplayString()} on instance {meta.This.ToString()}." );
            }

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [CompileTimeIf]
        public void InstanceMethod()
        {
            Console.WriteLine( "InstanceMethod" );
        }

        [CompileTimeIf]
        public static void StaticMethod()
        {
            Console.WriteLine( "StaticMethod" );
        }
    }
}