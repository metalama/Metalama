// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType;

#pragma warning disable CS0067, CS0169

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(AddAttributeAspect), typeof(IntroducingAspect), typeof(TypeIntroducingAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType;

internal class TypeIntroducingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass( "TestType" );
        result.AddAspect<IntroducingAspect>();
        result.AddAspect<AddAttributeAspect>();
    }
}

internal class IntroducingAspect : TypeAspect
{
    [Introduce]
    private int _field;

    [Introduce]
    private string? Property { get; set; }

    [Introduce]
    private long Method( string p ) => 0;

    [Introduce]
    public event EventHandler? Event;
}

internal class AddAttributeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var attribute = AttributeConstruction.Create( typeof(MyAttribute) );

        builder.IntroduceAttribute( attribute );

        foreach (var field in builder.Target.Fields)
        {
            builder.With( field ).IntroduceAttribute( attribute );
        }

        foreach (var property in builder.Target.Properties)
        {
            builder.With( property ).IntroduceAttribute( attribute );
        }

        foreach (var @event in builder.Target.Events)
        {
            builder.With( @event ).IntroduceAttribute( attribute );
        }

        foreach (var method in builder.Target.Methods)
        {
            builder.With( method ).IntroduceAttribute( attribute );
            builder.With( method.ReturnParameter ).IntroduceAttribute( attribute );

            foreach (var parameter in method.Parameters)
            {
                builder.With( parameter ).IntroduceAttribute( attribute );
            }
        }
    }
}

internal class MyAttribute : Attribute { }

// <target>
[TypeIntroducingAspect]
internal class C { }