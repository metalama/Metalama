// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Indexer.AccessorVisibility
{
    /*
     * Tests that accessor accessibility is correctly set when introducing indexers
     * with separate accessor templates that have different accessibility levels.
     * Regression test for issue #820.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ) },
                nameof(PublicGetter),
                nameof(PrivateSetter) );

            // Introduce an indexer with incomparable accessor accessibilities (protected vs internal).
            builder.IntroduceIndexer(
                new[] { ( typeof(string), "key" ) },
                nameof(ProtectedGetter),
                nameof(InternalSetter) );
        }

        [Template]
        public int PublicGetter()
        {
            Console.WriteLine( "Introduced getter" );

            return meta.Proceed();
        }

        [Template]
        private void PrivateSetter( int value )
        {
            Console.WriteLine( "Introduced setter" );
            meta.Proceed();
        }

        [Template]
        protected int ProtectedGetter()
        {
            return meta.Proceed();
        }

        [Template]
        internal void InternalSetter( int value )
        {
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
