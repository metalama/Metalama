// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_IntroductionResult;

// The result of an introduction is an adviser whose contributions are attributed to the aspect.

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.IntroduceMethod( nameof(this.Introduced) ).TestRegister( "introduced", this );

    [Template]
    private void Introduced() { }
}

// <target>
[TheAspect]
internal class C { }
