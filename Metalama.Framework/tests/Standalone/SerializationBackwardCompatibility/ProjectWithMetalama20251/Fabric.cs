using Metalama.Framework.Fabrics;
using Metalama.Extensions.Validation;

namespace ProjectWithMetalama20251;

public class Fabric : ProjectFabric
{    
    public override void AmendProject( IProjectAmender amender ) => 
        amender.SelectReflectionType( typeof(SomeReferencedClass) ).ValidateInboundReferences( this.ValidateReferences, ReferenceGranularity.Member );

    private void ValidateReferences(ReferenceValidationContext context)
    {        
    }
}