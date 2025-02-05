#if TESTRUNNER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using System.Linq;
#endif

namespace Metalama.Framework.Tests.AspectTests.Aspects.InvalidCode.MissingType;

public class C
{
#if TESTRUNNER // Avoid the code to be parsed in the IDE.
    [CompileTime]
    void M(IAspectBuilder<Foo> builder)
    {
        builder.Outbound.SelectMany(t => t);
    }
#endif
}