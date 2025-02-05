using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_Set
{
    internal class NotZeroAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == 0)
            {
                throw new ArgumentException();
            }
        }
    }

    // <target>
    internal class Target
    {
        private int q;

        [NotZero]
        public int this[ int x ]
        {
            set { }
        }

        [NotZero]
        public int this[ int x, int y ]
        {
            set => q = value + 1;
        }
    }
}