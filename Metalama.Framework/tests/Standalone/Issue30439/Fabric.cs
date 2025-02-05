using Metalama.Framework.Fabrics;
using Metalama.Framework.Aspects;
using System.Linq;

internal class Fabric : ProjectFabric
{
    public override void AmendProject(IProjectAmender project)
    {
        // Selecting the compiler-generated Main method causes an assertion failure.
        project.Outbound.SelectMany( p => p.Types.SelectMany( t => t.Methods ) ).AddAspectIfEligible<LogAttribute>();

        // Workaround:
        // project.With( p => p.Types.SelectMany( t => t.Methods.Where( m => m.Name != "<Main>$" ) ) ).AddAspect<LogAttribute>();
    }
}