[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Type")]
public class C1
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Type")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Type")] int p)
  {
  }
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    public override void AmendType(ITypeAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}