#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

/*
 * Tests that when parameters appended to two constructors with same non-optional parameters cause a need for
 * generated constructor that resolves ambiguity, this constructor is generated only once.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_Ambiguous;

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
    public TestClass( int param, int optional = 42 ) { }

    public TestClass( int param, string optional = "42" ) { }

    public void Foo()
    {
        _ = new TestClass( 42, 42 );
        _ = new TestClass( 42, "42" );
        _ = new TestClass( 42, optional: 42 );
        _ = new TestClass( 42, optional: "42" );
    }
}