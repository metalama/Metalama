// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_AdviserWith;

// An adviser created with With has the child declaration as its target, and its contributions are attributed to the aspect.

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.With( builder.Target.Methods.Single() ).TestRegister( "with", this );
}

// <target>
[TheAspect]
internal class C
{
    private void M() { }
}
