// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_StoredQuery_AfterBuildAspect_Throws;
using Metalama.Framework.Tests.ExtensionPoints;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_StoredQuery_AfterBuildAspect_Throws;

// An extension cannot capture the origin of a contribution made through a query after the aspect that created the query has finished
// executing. The capture throws an ObjectDisposedException, before the contribution is added.

internal class FirstAspect : TypeAspect
{
    public static IQuery<INamedType>? StoredQuery;

    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => StoredQuery = builder.Outbound;
}

internal class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => FirstAspect.StoredQuery!.TestRegisterQuery( "stale" );
}

// <target>
[FirstAspect]
[SecondAspect]
internal class C { }
