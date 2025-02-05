using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.TypeParameterBuilderPointer;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(M),
            buildMethod: methodBuilder =>
            {
                var typeParameter = methodBuilder.AddTypeParameter( "T" );
                typeParameter.TypeKindConstraint = TypeKindConstraint.Struct;
                var typeParameterPointer = typeParameter.MakePointerType();
                methodBuilder.AddParameter( "arg", typeParameterPointer );
            } );
    }

    [Template]
    private void M() { }
}

// <target>
[Aspect]
internal class TargetCode { }