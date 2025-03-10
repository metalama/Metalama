// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.EnumSerialization
{
    internal class LogAttribute : OverrideMethodAspect
    {
        // Template that overrides the methods to which the aspect is applied.
        public override dynamic? OverrideMethod()
        {
            var color = meta.CompileTime( ConsoleColor.Blue );

            Console.ForegroundColor = color;

            return meta.Proceed();
        }
    }

// <target>
    internal class TargetCode
    {
        [LogAttribute]
        private int Method( int a )
        {
            return a;
        }
    }
}