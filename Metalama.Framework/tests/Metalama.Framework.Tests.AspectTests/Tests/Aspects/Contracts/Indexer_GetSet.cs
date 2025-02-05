using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_GetSet
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
            get
            {
                return 42;
            }

            set { }
        }

        [NotZero]
        public int this[ int x, int y ]
        {
            get => q;
            set => q = value + 1;
        }
    }
}