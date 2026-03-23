// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithTypedConstantEnumInObjectArgs;

public enum MyEnum
{
    A,
    B
}

public class MyAttribute : Attribute
{
    public MyAttribute( MyEnum param ) { }

    public MyEnum NamedProp { get; set; }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Pass a TypedConstant wrapping an enum value through the object overload.
        // The TypedConstant's Type is MyEnum, but its Value is the underlying int.
        // Constructor resolution must use the TypedConstant's Type, not the Value's runtime type.
        var typedConstant = TypedConstant.Create( 1, typeof(MyEnum) );

        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object?[] { typedConstant },
                namedArguments: new KeyValuePair<string, object?>[] { new( "NamedProp", typedConstant ) } ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}
