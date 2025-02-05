using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RefMethod_Introduction_NotSupported;

public class TheAspect : TypeAspect
{
    [Introduce]
    private int _x;

    [Introduce]
    public ref int GetRef() => ref _x;
}

[TheAspect]
internal class C { }