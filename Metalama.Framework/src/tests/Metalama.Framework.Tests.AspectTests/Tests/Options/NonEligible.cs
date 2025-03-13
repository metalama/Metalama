// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.NonEligible;

public record class NonEligibleOptions : IHierarchicalOptions<INamedType>, IEligible<INamedType>
{
#if !NET5_0_OR_GREATER
    public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context ) => this;
#endif

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        return this;
    }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustBeStatic();
    }
}

public class C { }

public static class D { }

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectMany( c => c.Types ).SetOptions( _ => new NonEligibleOptions() );
    }
}