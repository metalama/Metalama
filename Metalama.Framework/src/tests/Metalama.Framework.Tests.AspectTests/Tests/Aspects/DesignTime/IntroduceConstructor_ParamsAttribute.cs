// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceConstructor_ParamsAttribute;

[RunTimeOrCompileTime]
public class MyAttribute : Attribute
{
    public MyAttribute( string a, params int[] p ) { }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Constructor),
            buildConstructor: c => c.AddAttribute(
                AttributeConstruction.Create( typeof(MyAttribute), new object?[] { "x", 1, 2 } ) ) );
    }

    [Template]
    public void Constructor( int x ) { }
}

// <target>
[Introduction]
internal partial class TargetClass { }
