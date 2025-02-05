#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTime.LocationInitializer
{
    internal class IdAttribute : TypeAspect
    {
        // Initializers should NOT make it into the partial class.

        [Introduce]
        public Guid Property { get; } = Guid.NewGuid();

        [Introduce]
        public Guid Field = Guid.NewGuid();
    }

    // <target>
    [Id]
    internal partial class TargetCode { }
}