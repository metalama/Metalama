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