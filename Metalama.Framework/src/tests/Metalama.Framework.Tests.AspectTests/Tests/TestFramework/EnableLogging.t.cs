using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.EnableLogging;
// Regression test for https://github.com/metalama/Metalama/issues/2224: the @EnableLogging option must not fail.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class C
{
  [TheAspect]
  public void M()
  {
    return;
  }
}