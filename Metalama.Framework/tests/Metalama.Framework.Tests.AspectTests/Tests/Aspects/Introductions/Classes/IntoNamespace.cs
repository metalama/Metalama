

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.IntoNamespace
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.ContainingNamespace ).IntroduceClass( "TestType" );
            builder.With( builder.Target.ContainingNamespace ).IntroduceClass( "TestType", buildType: t => t.AddTypeParameter( "T" ) );
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [IntroductionAttribute]
        public class TargetType { }
    }
}