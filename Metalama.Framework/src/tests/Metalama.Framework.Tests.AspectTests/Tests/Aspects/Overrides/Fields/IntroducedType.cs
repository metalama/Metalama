// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Fields.IntroducedType;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typeResult = builder.IntroduceClass( "TestType" );
        var methodResult = builder.With( typeResult.Declaration ).IntroduceField( nameof(IntroducedField) );

        builder.With( methodResult.Declaration ).Override( nameof(OverrideTemplate) );
    }

    [Template]
    public int IntroducedField;

    [Template]
    public dynamic? OverrideTemplate
    {
        get
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "Override" );
            meta.Proceed();
        }
    }
}

// <target>
[Aspect]
internal class Target { }