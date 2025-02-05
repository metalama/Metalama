#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

/*
 * Tests that when a parameter is appended to a constructor, the design-time pipeline generates a new constructor the allows settings the parameters in code.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter;

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
    public TestClass( int param ) { }

    public void Foo()
    {
        _ = new TestClass( 42 );
    }
}