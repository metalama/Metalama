// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public static TargetClass operator +(TargetClass x, int y)
        {
            Console.WriteLine("Original.");
            return x;
        }

        public static TargetClass operator +(TargetClass x)
        {
            Console.WriteLine("Original.");
            return x;
        }

        public static implicit operator TargetClass(int y)
        {
            Console.WriteLine("Original.");
            return new TargetClass();
        }
    }
}