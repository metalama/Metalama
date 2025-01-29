// Warning LAMA5005 on `ExecuteWithoutInvariants`: `The [SuspendInvariantsAttribute] aspect on method 'BaseClass.ExecuteWithoutInvariants()' is redundant the type 'BaseClass' does not contain any invariants.`
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics.Invariant_Suspend_Redundant;
public class BaseClass
{
  public int A { get; set; }
  public int B { get; set; }
  [SuspendInvariants]
  public void ExecuteWithoutInvariants()
  {
    this.A = -5;
    this.B = 5;
  }
}
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class Fabric : ProjectFabric
{
  public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}