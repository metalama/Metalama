// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.TemplateSetOnly
{
    public class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).OverrideAccessors( null, nameof(SetIndexer) );
            }
        }

        [Template]
        public void SetIndexer()
        {
            Console.WriteLine( "Override" );
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