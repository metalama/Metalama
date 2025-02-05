using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_SetParam
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

        public int this[ int x ]
        {
            get
            {
                return 42;
            }

            [param: NotZero]
            set { }
        }

        public int this[ int x, int y ]
        {
            get => q;

            [param: NotZero]
            set => q = value + 1;
        }
    }
}