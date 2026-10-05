// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Hook.Hook_WithLowLevelWeaver;
using Metalama.Framework.Tests.ExtensionPoints;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(RegularAspect1), typeof(WeaverAspect), typeof(RegularAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Hook.Hook_WithLowLevelWeaver;

// A low-level weaver executes between the two aspects. The transforming hook runs once, on the source compilation, so it receives the
// registration of the aspect that executes before the weaver. The registration of the aspect that executes after the weaver is not processed,
// and no diagnostic is reported for it.

[RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Hook.Hook_WithLowLevelWeaver.AspectWeaver" )]
internal class WeaverAspect : TypeAspect { }

[MetalamaPlugIn]
internal class AspectWeaver : IAspectWeaver
{
    public Task TransformAsync( AspectWeaverContext context ) => Task.CompletedTask;
}

internal class RegularAspect1 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRegister( "first" );
}

internal class RegularAspect2 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRegister( "second" );
}

// <target>
[RegularAspect1]
[WeaverAspect]
[RegularAspect2]
internal class C { }
