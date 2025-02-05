#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

/*
 * Tests that when a parameter is appended to a constructor with optional parameter, the design-time pipeline also generates
 * a "deambiguing" constructor without any optional parameter that prevents C# "ambiguous call" error cause by the constructor with new parameters.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptional;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "introduced1", typeof(int), TypedConstant.Create( 42 ) );
            builder.With( constructor ).IntroduceParameter( "introduced2", typeof(string), TypedConstant.Create( "42" ) );
        }
    }
}

// <target>
[Introduction]
internal partial class TestClass
{
    public TestClass( int param, int optParam = 42 ) { }

    public void Foo()
    {
        _ = new TestClass( 42 );
        _ = new TestClass( param: 42 );
        _ = new TestClass( 42, 42 );
        _ = new TestClass( 42, optParam: 42 );
        _ = new TestClass( optParam: 42, param: 13 );
    }
}