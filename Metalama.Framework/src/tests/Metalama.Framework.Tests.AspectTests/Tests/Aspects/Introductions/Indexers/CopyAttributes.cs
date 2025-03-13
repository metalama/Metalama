// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "y" ), ( typeof(int), "z" ) },
                nameof(GetTemplate),
                nameof(SetTemplate),
                args: new { x = 42 },
                buildIndexer: i => i.Type = TypeFactory.GetType( typeof(int) ) );
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public dynamic? GetTemplate( [CompileTime] int x, [Foo( 3 )] dynamic? y, [Foo( 4 )] dynamic? z )
        {
            return x + y + z;
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public void SetTemplate( [CompileTime] int x, [Foo( 3 )] dynamic? y, [Foo( 4 )] dynamic? z )
        {
            var w = x + y + z;
        }
    }

    public class FooAttribute : Attribute
    {
        public FooAttribute( int z ) { }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}