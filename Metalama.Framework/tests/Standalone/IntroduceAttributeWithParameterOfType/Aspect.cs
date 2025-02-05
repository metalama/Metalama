using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace MetaLamaTest;

[AttributeUsage( AttributeTargets.Class )]
public class MyAttribute : Attribute
{
    public MyAttribute( Type t )
    {
    }
}

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Advice.IntroduceAttribute(
            builder.Target,
            AttributeConstruction.Create(
                typeof( MyAttribute ),
                constructorArguments: [typeof( Target )] ) );
    }
}