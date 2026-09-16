// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System;

// Verifies the first acceptance criterion of issue #1942: a file that declares a run-time union and an aspect
// compiles, and the union does not reach the compile-time compilation. A union declared by a namespace is routed to
// the classification by its syntax kind, so this position was already covered when the test was written, and the
// test is the regression guard that the issue asks for rather than the reproduction of a failure.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.RunTimeUnionInAspectFile
{
    public union Shape( Circle, Rectangle );

    public record Circle( double Radius );

    public record Rectangle( double Width, double Height );

    public class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"{meta.Target.Method.Name} started." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Log]
        private Shape Method( Shape shape ) => shape;
    }
}
