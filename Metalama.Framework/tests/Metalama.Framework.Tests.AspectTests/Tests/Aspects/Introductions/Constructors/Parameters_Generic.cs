// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.Parameters_Generic;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: introduced => { introduced.AddParameter( "x", builder.Target ); } );

        // TODO: Other members.
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is introduced method." );

        meta.Proceed();
    }
}

// <target>
[Introduction]
internal class TargetClass<T> { }