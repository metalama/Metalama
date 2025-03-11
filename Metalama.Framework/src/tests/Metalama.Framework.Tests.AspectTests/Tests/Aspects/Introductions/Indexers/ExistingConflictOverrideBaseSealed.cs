// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.ExistingConflictOverrideBaseSealed
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ) },
                nameof(ExistingIndexer),
                nameof(ExistingIndexer),
                whenExists: OverrideStrategy.Override,
                buildIndexer: i => { i.Type = TypeFactory.GetType( typeof(int) ); } );
        }

        [Template]
        public dynamic? ExistingIndexer()
        {
            return meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public virtual int this[ int x ]
        {
            get
            {
                return 13;
            }

            set { }
        }
    }

    internal class DerivedClass : BaseClass
    {
        public sealed override int this[ int x ]
        {
            get
            {
                return 13;
            }

            set { }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass { }
}