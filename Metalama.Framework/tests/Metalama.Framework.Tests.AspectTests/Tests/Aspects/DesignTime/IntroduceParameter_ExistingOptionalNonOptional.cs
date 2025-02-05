#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

/*
 * Tests that when a parameter is appended to a constructor with optional parameter and a "deambiguing" constructor (a constructor with only mandatory parameters)
 * already exists, the design-time pipeline does not generate another "deambiguing" constructor.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptionalNonOptional;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors.Where( c => c.Parameters.Count == 2 ))
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