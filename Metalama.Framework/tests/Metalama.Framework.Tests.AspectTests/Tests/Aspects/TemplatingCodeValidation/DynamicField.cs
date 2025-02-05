using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.DynamicField;

#pragma warning disable CS0414

public class TheAspect : TypeAspect
{
    private dynamic f1 = null!;
    private dynamic? f2 = null;
    private dynamic[] f3 = null!;
    private List<dynamic> f4 = null!;
}