using System;
using Metalama.Framework.Fabrics;
namespace Metalama.Patterns.Contracts.AspectTests.Ambiguities.Unspecified_Strict;
public class C
{
  private int _a;
  [Positive]
  public int A
  {
    get
    {
      return _a;
    }
    set
    {
      if (value <= 0)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'A' property must be strictly greater than 0.");
      }
      _a = value;
    }
  }
  private int _b;
  [Negative]
  public int B
  {
    get
    {
      return _b;
    }
    set
    {
      if (value >= 0)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'B' property must be strictly less than 0.");
      }
      _b = value;
    }
  }
  private int _d;
  [GreaterThan(5)]
  public int D
  {
    get
    {
      return _d;
    }
    set
    {
      if (value <= 5)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'D' property must be strictly greater than 5.");
      }
      _d = value;
    }
  }
  private int _e;
  [LessThan(5)]
  public int E
  {
    get
    {
      return _e;
    }
    set
    {
      if (value >= 5)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'E' property must be strictly less than 5.");
      }
      _e = value;
    }
  }
}
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class Fabric : ProjectFabric
{
  public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}