// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithConstructorAndObjectArgs;

public class MyAttribute : Attribute
{
    public MyAttribute( int param1, string param2 ) { }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var attributeType = (INamedType) TypeFactory.GetType( typeof(MyAttribute) );
        var constructor = attributeType.Constructors.Single();

        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                constructor,
                constructorArguments: new object?[] { 42, "hello" } ) );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}
