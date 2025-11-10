// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.ConstFieldGetter;

public class TheMethodAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Overridden.");

        return meta.Proceed();
    }
}

public class TheFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectTypes(  )
            .SelectMany( x => x.FieldsAndProperties )
            .SelectMany( f => f.Accessors )
            .AddAspectIfEligible<TheMethodAspect>(  );
    }
}

// <target>
public class TheClass
{
    // C should not be modified.
    public const int C = 4;
    
    // The other fields should be modified.
    public int F;
    public int P { get; set; }
}