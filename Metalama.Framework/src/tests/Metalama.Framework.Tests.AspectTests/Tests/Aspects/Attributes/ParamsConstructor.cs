// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.ParamsConstructor;

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class MyAttribute : Attribute
{
    public MyAttribute( params int[] x ) { }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Zero parameter.
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );

        // One parameter.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { 1 } ),
            whenExists: OverrideStrategy.New );

        // Many parameters.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { 1, 2 } ),
            whenExists: OverrideStrategy.New );

        // Passing an array.
        builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object[] { new int[] { 1, 2, 3 } } ),
            whenExists: OverrideStrategy.New );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}