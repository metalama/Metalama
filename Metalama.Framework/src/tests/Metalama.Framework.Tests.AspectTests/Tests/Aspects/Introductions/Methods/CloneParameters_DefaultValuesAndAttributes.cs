// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.Methods.CloneParameters_DefaultValuesAndAttributes;

/*
 * Copies the parameters of a method twice: once with their attributes, default values and params modifier, and once without them.
 */

public class CopyAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var source = builder.Target.Methods.OfName( "Source" ).Single();

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: method =>
            {
                method.Name = "CopyWithAll";

                foreach ( var parameter in source.Parameters )
                {
                    method.AddParameter( parameter, includeCustomAttributes: true, includeDefaultValues: true );
                }
            } );

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: method =>
            {
                method.Name = "CopyWithNone";

                foreach ( var parameter in source.Parameters )
                {
                    method.AddParameter( parameter );
                }
            } );
    }

    [Template]
    public void Template() { }
}

// <target>
[Copy]
public class Target
{
    public void Source( [Description( "The count." )] int count = 5, string? name = null, params string[] rest ) { }
}
