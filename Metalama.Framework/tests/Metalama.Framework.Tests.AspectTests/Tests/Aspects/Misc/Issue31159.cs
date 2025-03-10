// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31159;

[RunTimeOrCompileTime]
public class DerivedAspect : BaseAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        base.BuildEligibility( builder );
    }

    public override void Validate( dynamic? value )
    {
        Console.WriteLine( "Again" );
    }
}

// <target>
public interface I
{
    void M( [DerivedAspect] int x );
}

// <target>
public class C : I
{
    public void M( [DerivedAspect] int x ) { }
}