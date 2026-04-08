using System;
using System.Collections.Generic;
using Metalama.Framework.Fabrics;
namespace Metalama.Patterns.Contracts.AspectTests.Fabric_Project_Required_Collection
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Fabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class TestClass
  {
    // [Required] on collection parameter should NOT trigger LAMA5003.
    public void ProcessItems([Required] ICollection<string> items)
    {
      if (items == null !)
      {
        throw new ArgumentNullException("items", "The 'items' parameter is required.");
      }
    }
  }
}
