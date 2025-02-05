using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass( "PrivateType", buildType: t => { t.Accessibility = Code.Accessibility.Private; } );
        builder.IntroduceClass( "ProtectedType", buildType: t => { t.Accessibility = Code.Accessibility.Protected; } );

        builder.IntroduceClass(
            "PrivateProtectedType",
            buildType: t => { t.Accessibility = Code.Accessibility.PrivateProtected; } );

        builder.IntroduceClass(
            "ProtectedInternalType",
            buildType: t => { t.Accessibility = Code.Accessibility.ProtectedInternal; } );

        builder.IntroduceClass( "InternalType", buildType: t => { t.Accessibility = Code.Accessibility.Internal; } );
        builder.IntroduceClass( "PublicType", buildType: t => { t.Accessibility = Code.Accessibility.Public; } );
        builder.IntroduceClass( "UndefinedType", buildType: t => { t.Accessibility = Code.Accessibility.Undefined; } );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }