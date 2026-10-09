// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.Methods.CloneParameter_NameCollision_Error;

/*
 * Copies the same parameter twice, which throws because a parameter of the same name already exists.
 */

public class CopyAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var parameter = builder.Target.Methods.OfName( "Source" ).Single().Parameters[0];

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: method =>
            {
                method.AddParameter( parameter );
                method.AddParameter( parameter );
            } );
    }

    [Template]
    public void Template() { }
}

// <target>
[Copy]
public class Target
{
    public void Source( int value ) { }
}
