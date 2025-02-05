#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
// @RequiredConstant(NETCOREAPP3_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.EmptyType;

internal class TheAspect : TypeAspect
{
    [Introduce]
    private void M() { }
}

// <target>
[TheAspect]
internal class C;

// <target>
[TheAspect]
internal struct S;

// Not new in C# 12, included for completeness.
// <target>
[TheAspect]
internal record R;

// <target>
[TheAspect]
internal interface I;

#endif