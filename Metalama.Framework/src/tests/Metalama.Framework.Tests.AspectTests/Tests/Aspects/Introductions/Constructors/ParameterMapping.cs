// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.ParameterMapping;

/*
 * Verifies that template parameters are correctly mapped by name.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(InvertedParameterNames),
            buildConstructor: b =>
            {
                b.Parameters[0].Name = "y";
                b.Parameters[1].Name = "x";
                b.AddParameter( "z", typeof(int) );
            } );
    }

    [Template]
    public void InvertedParameterNames( int x, string y )
    {
        _ = x + y.Length;
    }
}

// <target>
[Introduction]
internal class TargetClass { }