#if TEST_OPTIONS
// @Include(Imported\Imported.cs)
#endif
#pragma warning disable 169

using Metalama.Framework.Tests.AspectTests.Tests.TestFramework.Imported;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework
{
    [ImportedAspect]
    public class IncludeDirective { }
}