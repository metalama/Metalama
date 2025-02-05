using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Accessibility;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceInterface( "ITestPrivate", buildType: b => { b.Accessibility = Code.Accessibility.Private; });
        builder.IntroduceInterface( "ITestPrivateProtected", buildType: b => { b.Accessibility = Code.Accessibility.PrivateProtected; });
        builder.IntroduceInterface( "ITestProtected", buildType: b => { b.Accessibility = Code.Accessibility.Protected; });
        builder.IntroduceInterface( "ITestProtectedInternal", buildType: b => { b.Accessibility = Code.Accessibility.ProtectedInternal; });
        builder.IntroduceInterface( "ITestInternal", buildType: b => { b.Accessibility = Code.Accessibility.Internal; });
        builder.IntroduceInterface( "ITestPublic", buildType: b => { b.Accessibility = Code.Accessibility.Public; });
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }