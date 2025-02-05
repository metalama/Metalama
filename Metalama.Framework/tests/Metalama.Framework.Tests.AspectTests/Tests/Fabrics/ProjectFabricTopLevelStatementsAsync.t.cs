using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
namespace Metalama.Framework.IntegrationTests.Aspects.Fabrics.ProjectFabricTopLevelStatementsAsync
{
  /*
     * Tests that applying an override on all methods does not target the async Main method containing top-level statements.
     */
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class OverrideAttribute : OverrideMethodAspect
  {
    public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class MyProjectFabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}