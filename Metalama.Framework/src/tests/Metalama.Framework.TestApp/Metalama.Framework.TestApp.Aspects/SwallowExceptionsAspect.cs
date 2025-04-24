// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.TestApp.Aspects
{
    public class SwallowExceptionsAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            try
            {
                return meta.Proceed();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Metalama caught: " + ex);
                return default;
            }
        }
    }
}
