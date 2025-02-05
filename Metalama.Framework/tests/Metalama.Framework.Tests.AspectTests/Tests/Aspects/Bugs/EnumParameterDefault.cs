using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.EnumParameterDefault;

class Aspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: methodBuilder => methodBuilder.AddParameter("p", typeof(E), RefKind.None, TypedConstant.Create(1, typeof(E))));
    }

    [Template]
    void Template() { }
}

// <target>
[Aspect]
class Foo
{
}

enum E
{
    A = 1
}