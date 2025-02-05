using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30976;
/*
 * Tests that MyAspect is wrapped with `#pragma warning`, but it does
not interfere with the #if/#endif directives above the aspect.
 */
#if YES
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class MyAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
#endif
public class X
{
}