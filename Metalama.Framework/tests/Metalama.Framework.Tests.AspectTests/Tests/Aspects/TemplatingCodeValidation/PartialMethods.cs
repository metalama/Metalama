using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.PartialMethods;

public partial class Aspect : IAspect
{
    [Template]
    partial void Template(int i);

    [Template]
    private partial int Template(string s);

    private partial int Template(string s) => 0;

    partial void RunTimeMethod(int i);

    [CompileTime]
    partial void CompileTimeMethod(int i);
}