// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_AfterBuildAspect_Throws;
using Metalama.Framework.Tests.ExtensionPoints;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_AfterBuildAspect_Throws;

// An adviser cannot be used by an extension after the aspect that received it has finished executing.

internal class FirstAspect : TypeAspect
{
    public static IAdviser<INamedType>? StoredAdviser;

    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => StoredAdviser = builder;
}

internal class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => FirstAspect.StoredAdviser!.TestRegister( "stale" );
}

// <target>
[FirstAspect]
[SecondAspect]
internal class C { }
