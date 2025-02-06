using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
namespace Metalama.Patterns.Contracts.AspectTests.DisablePostconditions;
public class C
{
  private string _p = "x";
  [NotEmpty(Direction = ContractDirection.Both)]
  public string P
  {
    get
    {
      return _p;
    }
    set
    {
      if (value.Length <= 0)
      {
        throw new ArgumentException("The 'P' property must not be null or empty.", "value");
      }
      _p = value;
    }
  }
  public void M([NotEmpty] string a, [NotEmpty] out string b)
  {
    if (a.Length <= 0)
    {
      throw new ArgumentException("The 'a' parameter must not be null or empty.", "a");
    }
    b = "b";
  }
}
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class Fabric : ProjectFabric
{
  public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}