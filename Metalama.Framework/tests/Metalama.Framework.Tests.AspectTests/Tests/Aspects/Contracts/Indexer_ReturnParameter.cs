using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS0169, CS0649

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_ReturnParameter
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
            [return: NotZero]
            get
            {
                Console.WriteLine( "Original body" );

                return 42;
            }
        }

        public int this[ int x, int y ]
        {
            [return: NotZero]
            get
            {
                Console.WriteLine( "Original body" );

                return 42;
            }
        }
    }
}