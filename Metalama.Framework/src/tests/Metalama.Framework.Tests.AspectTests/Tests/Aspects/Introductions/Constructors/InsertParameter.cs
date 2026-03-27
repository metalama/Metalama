// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.InsertParameter;

/*
 * Tests that InsertParameter works correctly on introduced constructors,
 * inserting parameters before the template's own parameters.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c =>
            {
                // Insert a parameter at the start, before the template's 'message' parameter.
                c.InsertParameter( 0, "id", typeof(int) );
            } );
    }

    [Template]
    public void Template( string message )
    {
        Console.WriteLine( message );
    }
}

// <target>
[Introduction]
internal class TargetClass { }
