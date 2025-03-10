// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS0169, CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_InvalidDirections
{
    internal class NotZeroAttribute : ContractAspect
    {
        public NotZeroAttribute( ContractDirection direction ) : base( direction ) { }

        public override void Validate( dynamic? value )
        {
            if (value == 0)
            {
                throw new ArgumentException();
            }
        }
    }

    internal class Target
    {
        // All these targets are invalid.

        [NotZero( ContractDirection.Input )]
        public int this[ int x ]
        {
            get
            {
                return 42;
            }
        }

        [NotZero( ContractDirection.Both )]
        public int this[ int x, int y ]
        {
            get
            {
                return 42;
            }
        }

        [NotZero( ContractDirection.Output )]
        public int this[ int x, int y, int z ]
        {
            set { }
        }
    }
}