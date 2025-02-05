#if TEST_OPTIONS
// @IgnoredDiagnostic(CS0618)
#endif
#pragma warning disable CS0618

using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Fabrics.TransitiveProjectFabric
{
    public class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.Project.Extension<Configuration>().Message = "Configured";
        }
    }

    // <target>
    public class TargetCode
    {
        public int Method() => 0;
    }
}