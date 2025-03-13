// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.Simple
{
    public class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer) );
            }
        }

        [Template]
        public dynamic? GetIndexer()
        {
            Console.WriteLine( $"Override [{meta.Target.Indexer.Parameters[0].Value}]" );

            return meta.Proceed();
        }

        [Template]
        public void SetIndexer()
        {
            Console.WriteLine( $"Override [{meta.Target.Indexer.Parameters[0].Value}]" );
            meta.Proceed();
        }
    }

    // <target>
    [Test]
    internal class TargetClass
    {
        public int this[ int x ]
        {
            get
            {
                Console.WriteLine( "Original" );

                return x;
            }

            set
            {
                Console.WriteLine( "Original" );
            }
        }
    }
}