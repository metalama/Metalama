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