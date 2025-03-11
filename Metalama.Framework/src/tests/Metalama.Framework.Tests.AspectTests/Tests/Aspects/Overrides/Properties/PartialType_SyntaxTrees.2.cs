// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.PartialType_SyntaxTrees
{
    // <target>
    internal partial class TargetClass
    {
        public int TargetProperty3
        {
            get
            {
                Console.WriteLine("This is TargetProperty3.");
                return 42;
            }

            set => Console.WriteLine("This is TargetProperty3.");
        }
    }
}