#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptionalIntroducedParameterless;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ConstructorIntroductionAttribute), typeof(ParameterIntroductionAttribute) )]

/*
 * Tests that when a parameter is appended to a constructor with optional parameter and a "deambiguing" constructor (a constructor with only mandatory parameters)
 * is introduced by an aspect, the design-time pipeline does not generate another "deambiguing" constructor.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptionalIntroducedParameterless;

public class ConstructorIntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.AddParameter( "x", typeof(int) ); } );
    }

    [Template]
    public void Template() { }
}

public class ParameterIntroductionAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter( "introduced1", typeof(int), TypedConstant.Create( 42 ) );
        builder.IntroduceParameter( "introduced2", typeof(string), TypedConstant.Create( "42" ) );
    }
}

// <target>
[ConstructorIntroduction]
internal partial class TestClass
{
    [ParameterIntroduction]
    public TestClass( int param, int optParam = 42 ) { }
}