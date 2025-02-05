#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32522;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Immutable;


[CompileTime]
class C
{
    void M()
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add( "");
        builder.ToImmutable();
    }
}