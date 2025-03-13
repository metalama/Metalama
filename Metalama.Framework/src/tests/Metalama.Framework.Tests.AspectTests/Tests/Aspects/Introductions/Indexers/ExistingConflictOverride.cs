// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.ExistingConflictOverride
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ) },
                nameof(ExistingBaseIndexer),
                nameof(ExistingBaseIndexer),
                whenExists: OverrideStrategy.Override,
                buildIndexer: i => { i.Type = TypeFactory.GetType( typeof(int) ); } );

            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ), ( typeof(int), "y" ) },
                nameof(ExistingIndexer),
                nameof(ExistingIndexer),
                whenExists: OverrideStrategy.Override,
                buildIndexer: i => { i.Type = TypeFactory.GetType( typeof(int) ); } );

            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ), ( typeof(int), "y" ), ( typeof(int), "z" ) },
                nameof(NotExistingIndexer),
                nameof(NotExistingIndexer),
                whenExists: OverrideStrategy.Override,
                buildIndexer: i => { i.Type = TypeFactory.GetType( typeof(int) ); } );
        }

        [Template]
        public dynamic? ExistingBaseIndexer()
        {
            meta.InsertComment( "Call the base indexer." );
            Console.WriteLine( $"This is introduced indexer {meta.Target.Indexer.Parameters[0].Value}." );

            return meta.Proceed();
        }

        [Template]
        public dynamic? ExistingIndexer()
        {
            meta.InsertComment( "Return a constant/do nothing." );
            Console.WriteLine( $"This is introduced indexer {meta.Target.Indexer.Parameters[0].Value}." );

            return meta.Proceed();
        }

        [Template]
        public dynamic? NotExistingIndexer()
        {
            meta.InsertComment( "Return default value/do nothing." );
            Console.WriteLine( $"This is introduced indexer {meta.Target.Indexer.Parameters[0].Value}." );

            return meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public virtual int this[ int x ]
        {
            get
            {
                return 27;
            }
            set { }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass
    {
        public virtual int this[ int x, int y ]
        {
            get
            {
                return 27;
            }
            set { }
        }
    }
}