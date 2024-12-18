#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using Metalama.Framework.Fabrics;
using Metalama.Framework.Validation;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Validation.UserException;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectMany(p => p.Types).Where(t => t.Name == nameof(C)).Validate((in DeclarationValidationContext x) => throw new Exception());
    }
}

internal class C { }