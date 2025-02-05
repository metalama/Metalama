#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_IntroducedConstructor;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ConstructorIntroductionAttribute), typeof(ParameterIntroductionAttribute) )]

/*
 * Tests that when a parameter is appended to an introduced constructor, the design-time pipeline generates a correct constructor with optional parameters.
 */

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_IntroducedConstructor;

public class ConstructorIntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { } );

        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.AddParameter( "p", typeof(int) ); } );
    }

    [Template]
    public void Template() { }
}

public class ParameterIntroductionAttribute : TypeAspect
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
[ConstructorIntroduction]
[ParameterIntroduction]
internal partial class TestClass { }