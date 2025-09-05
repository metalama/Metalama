// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexer.Covariant
{
    internal class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer( typeof(int), nameof(this.Indexer), null,  whenExists: OverrideStrategy.Override );
        }

        [Template]
        public DerivedClass Indexer(int i) => null;
    }

    internal class BaseClass
    {
        public virtual BaseClass this[int i] => null;
    }
    
    // <target>
    [Introduction]
    internal abstract class DerivedClass : BaseClass { }
}