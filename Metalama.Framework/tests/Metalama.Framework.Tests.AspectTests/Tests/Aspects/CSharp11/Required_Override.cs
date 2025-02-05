#if TEST_OPTIONS
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if NET7_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.Required_Override;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => meta.Proceed();
        set => meta.Proceed();
    }
}


// <target>
public class C
{
    [TheAspect]
    public required int Field;
    
    [TheAspect]
    public required int Property { get; set; }
}

#endif