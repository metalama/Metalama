// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithTypedConstantInObjectArgs;

public enum MyEnum
{
    A,
    B
}

public class MyAttribute : Attribute
{
    public MyAttribute( int param ) { }

    public int NamedProp { get; set; }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Pass a TypedConstant as a constructor argument through the object overload.
        var typedConstant = TypedConstant.Create( 42, typeof(int) );

        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object?[] { typedConstant },
                namedArguments: new KeyValuePair<string, object?>[] { new( "NamedProp", TypedConstant.Create( 99, typeof(int) ) ) } ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}
