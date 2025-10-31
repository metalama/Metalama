// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_IntroduceMethod;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );
        var collectionType = TypeFactory.GetNamedType( typeof(IEnumerable<>) ).MakeGenericInstance( typeof(int) );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );
        extensionBlockBuilder.IntroduceMethod( nameof(this.SomeMethod) );
        extensionBlockBuilder.IntroduceMethod( nameof(SomeStaticMethod) );
        extensionBlockBuilder.IntroduceBinaryOperator( nameof(SomeOperator), collectionType, TypeFactory.GetNamedType( typeof(int) ), collectionType, OperatorKind.Addition );
    }

    [Template]
    public int SomeMethod( int a, int b )
    {
        Console.WriteLine( $"Member: {meta.Target.Method}" );
        Console.WriteLine( $"Type: {meta.Target.Type}" );
        
        return meta.Receiver + a + b;
    }
    
    [Template]
    public static int SomeStaticMethod( int a, int b )
    {
        Console.WriteLine( $"Member: {meta.Target.Method}" );
        Console.WriteLine( $"Type: {meta.Target.Type}" );

        return  a + b;
    }
    
    [Template]
    public static dynamic? SomeOperator( dynamic a, dynamic b )
    {
        Console.WriteLine( $"Member: {meta.Target.Method}" );
        Console.WriteLine( $"Type: {meta.Target.Type}" );

        return default;
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