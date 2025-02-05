using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.FieldModifiers;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceField( "unmodifiedField", typeof(int), buildField: field => field.Writeability = Writeability.All );
        builder.IntroduceField( "readonlyField", typeof(int), buildField: field => field.Writeability = Writeability.ConstructorOnly );

        builder.IntroduceField(
            "constField",
            typeof(int),
            buildField: field =>
            {
                field.Writeability = Writeability.None;
                field.InitializerExpression = ExpressionFactory.Literal( 42 );
            } );
    }
}

// <target>
[Aspect]
internal class TargetClass { }