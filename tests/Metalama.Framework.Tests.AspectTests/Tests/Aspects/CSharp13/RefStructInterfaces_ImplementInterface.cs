#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
// @RequiredConstant(NET9_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER && NET9_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefStructInterfaces_ImplementInterface;

class TheAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        builder.ImplementInterface(typeof(I));
    }

    [InterfaceMember]
    public void M<T>() where T : I, new(), allows ref struct
    {
        var x = new T();
        x.M<T>();
    }
}

interface I
{
    void M<T>() where T : I, new(), allows ref struct;
}

// <target>
[TheAspect]
ref struct S
{
}

#endif