// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.ChildAspect.Outbound_AfterFailedBuildAspect_Throws;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ChildAspect.Outbound_AfterFailedBuildAspect_Throws;

// The BuildAspect method of FirstAspect stores its query and then throws. SecondAspect uses the query afterwards. Adding a child aspect
// through it must fail as well, because the failed aspect produces no result that could contain the contributor.

internal class FirstAspect : TypeAspect
{
    public static IQuery<INamedType>? StoredQuery;

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        StoredQuery = builder.Outbound;

        throw new InvalidOperationException( "FirstAspect failed." );
    }
}

internal class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        FirstAspect.StoredQuery!.SelectMany( t => t.Methods ).AddAspect<ChildAspect>();
    }
}

internal class ChildAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}

// <target>
[FirstAspect]
[SecondAspect]
internal class C
{
    private void M() { }
}
