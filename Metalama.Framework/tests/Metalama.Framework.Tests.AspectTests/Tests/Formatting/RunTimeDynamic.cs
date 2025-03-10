// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.RunTimeDynamic
{
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var x = meta.Target.Parameters[0].Value;
            Console.WriteLine(x);

            return meta.Proceed();
        }
    }

    class TargetCode
    {
        [Aspect]
        void M(int a) {}
    }



}