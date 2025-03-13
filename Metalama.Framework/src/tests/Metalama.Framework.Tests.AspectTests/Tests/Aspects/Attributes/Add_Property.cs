// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Property;

public class MyAttribute : Attribute { }

public class MyAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );

        if (builder.Target.GetMethod != null)
        {
            builder.With( builder.Target.GetMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }

        if (builder.Target.SetMethod != null)
        {
            builder.With( builder.Target.SetMethod ).IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
        }
    }
}

// <target>
internal class C
{
    [MyAspect]
    private int Property1 { get; set; }
}