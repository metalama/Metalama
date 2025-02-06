using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
namespace Metalama.Patterns.Contracts.AspectTests.DisablePreconditions;
public class C
{
  private string _p = "x";
  [NotEmpty(Direction = ContractDirection.Both)]
  public string P
  {
    get
    {
      var returnValue = _p;
      if (returnValue.Length <= 0)
      {
        throw new PostconditionViolationException("The 'P' property must not be null or empty.");
      }
      return returnValue;
    }
    set
    {
      _p = value;
    }
  }
  public void M([NotEmpty] string a, [NotEmpty] out string b)
  {
    b = "b";
    if (b.Length <= 0)
    {
      throw new PostconditionViolationException("The 'b' parameter must not be null or empty.");
    }
  }
}
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class Fabric : ProjectFabric
{
  public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}