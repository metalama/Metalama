using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Field.Programmatic
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceField( "IntroducedField", typeof(int) );

            builder.IntroduceField(
                "IntroducedField_Static",
                typeof(int),
                buildField: p => { p.IsStatic = true; } );

            // TODO: Other members.
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}