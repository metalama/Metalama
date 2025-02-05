#if TEST_OPTIONS
// @Include(../../Common/_ImplementInterfaceAdviceResultExtensions.cs)
#endif

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.InvokingIntroduced;

internal class IntroduceAndInvokeAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var introducedMember = builder.IntroduceMethod( nameof(Introduce) );

        builder.IntroduceMethod(
            nameof(Invoke),
            buildMethod: method => method.Name = "Invoke0",
            args: new { introduced = introducedMember.Declaration } );

        var interfaceImplementation = builder.ImplementInterface( typeof(IFoo) );

        builder.IntroduceMethod(
            nameof(Invoke),
            args: new { introduced = interfaceImplementation.GetObsoleteInterfaceMembers().Single().TargetMember } );
    }

    [InterfaceMember]
    public void Bar() { }

    [Template]
    public void Introduce() { }

    [Template]
    public void Invoke( IMethod introduced )
    {
        introduced.Invoke();
    }
}

internal interface IFoo
{
    void Bar();
}

// <target>
[IntroduceAndInvoke]
internal class Target { }