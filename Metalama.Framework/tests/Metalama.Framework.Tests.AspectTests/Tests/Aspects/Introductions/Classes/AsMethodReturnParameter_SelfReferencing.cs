using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AsMethodReturnParameter_SelfReferencing;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass( "IntroducedNestedType" );

        result.IntroduceMethod(
                nameof(MethodTemplate),
                buildMethod: b =>
                {
                    b.Name = "Method";
                    b.ReturnType = result.Declaration;
                } );
    }

    [Template]
    public dynamic? MethodTemplate() 
    {
        return default;
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }