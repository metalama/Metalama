// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.ReplaceImplicitDefaultConstructor;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.IsStatic = true; } );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "Before" );
        meta.Proceed();
        Console.WriteLine( "After" );
    }
}

// <target>
[Introduction]
internal class TargetClass
{
    public static int F = 42;

    public static int P { get; set; } = 42;
}