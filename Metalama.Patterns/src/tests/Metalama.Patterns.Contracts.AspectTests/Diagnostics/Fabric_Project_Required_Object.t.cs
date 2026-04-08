// Warning LAMA5003 on `foo`: `The [RequiredAttribute] contract is redundant because the [NotNull] contract is automatically added by a fabric.`
using System;
using Metalama.Framework.Fabrics;
namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics.Fabric_Project_Required_Object
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Fabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class TestClass
  {
    // [Required] on object parameter SHOULD trigger LAMA5003 because [Required]
    // and [NotNull] have identical behavior for non-string, non-collection types.
    public void DoSomething([Required] object foo)
    {
      if (foo == null !)
      {
        throw new ArgumentNullException("foo", "The 'foo' parameter is required.");
      }
    }
  }
}
