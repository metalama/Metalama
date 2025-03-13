// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public int ExistingProperty
        {
            get
            {
                Console.WriteLine("Original");
                return 42;
            }
            set
            {
                Console.WriteLine("Original");
            }
        }

        public int ExistingProperty_Expression => 42;

        public int ExistingProperty_Auto { get; set; }

        public int ExistingProperty_AutoInitializer { get; set; } = 42;

        public int ExistingProperty_InitOnly
        {
            get
            {
                Console.WriteLine("Original");
                return 42;
            }
            init
            {
                Console.WriteLine("Original");
            }
        }

        public IEnumerable<int> ExistingProperty_Iterator
        {
            get
            {
                Console.WriteLine("Original");
                yield return 42;
            }
        }
    }
}