// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Issue30200
{
    public class MyAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // The following line should generate a platform-specific type reference (because of the assembly reference),
            // but the platform should be ignored when resolving the reference.
            
            var isVoid = meta.Target.Method.ReturnType.Is(typeof(void));
            Console.WriteLine($"isVoid={isVoid}");
            return meta.Proceed();
        }
    }

}