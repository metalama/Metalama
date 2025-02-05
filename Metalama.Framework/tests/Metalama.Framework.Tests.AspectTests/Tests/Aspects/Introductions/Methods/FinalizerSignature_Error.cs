using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.FinalizerSignature_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: methodBuilder => { methodBuilder.Name = "Finalize"; } );
        }

        [Template]
        public void Template() { }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        ~TargetClass() { }
    }
}