using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Fabrics.TwoTransitiveProjectFabrics
{
    public class TransitiveFabric1 : Framework.Fabrics.TransitiveProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
           
        }
    }
    
    public class TransitiveFabric2 : Framework.Fabrics.TransitiveProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
           
        }
    }

}