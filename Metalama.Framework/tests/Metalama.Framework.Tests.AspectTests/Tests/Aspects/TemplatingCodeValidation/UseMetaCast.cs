namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseMetaCast;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

internal class TestAttribute : TypeAspect
{
    [Template]
    public dynamic? MyTemplate()
    {
        var x = meta.Proceed();

        return meta.Cast( meta.Target.Type, x );
    }
}