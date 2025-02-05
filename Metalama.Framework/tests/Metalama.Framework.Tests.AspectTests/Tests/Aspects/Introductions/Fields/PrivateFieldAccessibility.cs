using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.PrivateFieldAccessibility;

#pragma warning disable CS0414 // The field 'IntroducePrivateFieldAttribute._field' is assigned but its value is never used

public class IntroducePrivateFieldAttribute : IAspect
{
    [Introduce]
    private readonly RunTimeOnlyClass _field = null!;
}

internal class RunTimeOnlyClass { }