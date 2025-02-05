using Metalama.Framework.Tests.AspectTests.Tests.Options;
#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

[assembly: MyOptions( "Attribute.Assembly" )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.Attributes;

[MyOptions( "Attribute.C" )]
[ShowOptionsAspect]
public class C
{
    [ShowOptionsAspect]
    [MyOptions( "Attribute.M" )]
    public void M( int p ) { }
}

[ShowOptionsAspect]
public class D { }