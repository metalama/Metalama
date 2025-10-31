// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_IntroduceAutoProperty;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );
        var collectionType = TypeFactory.GetNamedType( typeof(IEnumerable<>) ).MakeGenericInstance( typeof(int) );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );
        extensionBlockBuilder.IntroduceAutomaticProperty( "SomeProperty", typeof(string) );


    }

    [Template]
    public static int SomeStaticProperty
    {
        get => default;
        set => Console.WriteLine( "write" );
    }

}

// <target>
[TheAspect]
internal static class C
{
    extension( int test )
    {
    }
}


#endif