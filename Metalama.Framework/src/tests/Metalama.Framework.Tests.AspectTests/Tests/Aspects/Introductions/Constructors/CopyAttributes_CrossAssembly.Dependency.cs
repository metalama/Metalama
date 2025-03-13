// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.CopyAttributes_CrossAssembly;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor( nameof(Constructor), args: new { S = typeof(int), x = 42 } );
    }

    [Template]
    [Foo( 1 )]
    public void Constructor<[CompileTime] S>( [CompileTime] int x, [Foo( 2 )] int y, [Foo( 3 )] int z )
    {
        Console.WriteLine( "This is introduced constructor." );
    }
}

public class FooAttribute : Attribute
{
    public FooAttribute( int x ) { }
}