using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.TypeParameterBuilderNullable;

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
                var nullableTypeParameter =  typeParameter.ToNullable();
                methodBuilder.AddParameter( "arg", nullableTypeParameter );
            } );
    }

    [Template]
    private void M() { }
}

// <target>
[Aspect]
internal class TargetCode { }