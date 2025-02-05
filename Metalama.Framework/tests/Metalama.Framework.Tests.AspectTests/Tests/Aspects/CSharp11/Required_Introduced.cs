#if TEST_OPTIONS
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if NET7_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.Required_Introduced;

public class TheAspect : TypeAspect
{
    [Introduce( IsRequired = true )]
    public int Field;

    [Introduce( IsRequired = true )]
    public int Property { get; set; }
}

// <target>
[TheAspect]
public class C { }

#endif