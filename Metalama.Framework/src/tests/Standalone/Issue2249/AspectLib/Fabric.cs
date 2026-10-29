using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace AspectLib;

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectTypes().SelectMany( t => t.Methods ).Where( m => m.Name == "Subtract" ).AddAspect<LogAttribute>();
    }
}
