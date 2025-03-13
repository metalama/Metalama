// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.TypedConstant_;

public enum MyRunTimeEnum
{
    A,
    B
}

public class MyAttribute : Attribute
{
    private int _property;

    public MyRunTimeEnum Property
    {
        get => (MyRunTimeEnum)_property;
        set => _property = (int)value;
    }
}

public class MyAspect : MethodAspect
{
    public int Property { get; set; }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                namedArguments: new KeyValuePair<string, object?>[] { new( "Property", TypedConstant.Create( Property, typeof(MyRunTimeEnum) ) ) } ) );
    }
}

// <target>
internal class C
{
    [MyAspect( Property = (int)MyRunTimeEnum.B )]
    private void M() { }
}