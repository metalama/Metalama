// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Applying.AppliedToNamespace;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(MyTypeAspect), typeof(MyNamespaceAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Applying.AppliedToNamespace;

public class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.AddAspect<MyNamespaceAspect>();
    }
}

public class MyNamespaceAspect : IAspect<INamespace>
{
    public void BuildAspect( IAspectBuilder<INamespace> builder )
    {
        builder.Outbound.SelectMany( ns => ns.Types ).AddAspectIfEligible<MyTypeAspect>();
    }

    public void BuildEligibility( IEligibilityBuilder<INamespace> builder ) { }
}

internal class MyTypeAspect : TypeAspect
{
    [Introduce]
    public void IntroducedMethod() { }
}

// <target>
internal class C { }