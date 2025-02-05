#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
// @RequiredConstant(NET9_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER && NET9_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefStructInterfaces_Basic;

class TheAspect : TypeAspect
{
    [Introduce]
    void M<T>() where T : allows ref struct
    {
    }
}

// <target>
[TheAspect]
class C
{
}

#endif