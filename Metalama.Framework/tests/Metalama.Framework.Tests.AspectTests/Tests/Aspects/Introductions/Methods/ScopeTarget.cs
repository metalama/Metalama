using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.ScopeTarget;

public class TheAspect : Attribute, IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }

    [Introduce( Scope = IntroductionScope.Target )]
    public void IntroducedMethod() { }
}

[TheAspect]
public class InstanceClass { }

[TheAspect]
public static class StaticClass { }

public class Class1
{
    [TheAspect]
    public void InstanceMember() { }
}

public class Class3
{
    // The class is intentionally not static.

    [TheAspect]
    public static void StaticMember() { }
}