using System;
namespace Metalama.Patterns.Contracts.AspectTests;
public sealed class RangeOnlyTestsRequiredBounds
{
  public void Negative([NonPositive] int x)
  {
    if (x > 0)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be less than or equal to 0.");
    }
  }
  public void Positive([NonNegative] int x)
  {
    if (x < 0)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be greater than or equal to 0.");
    }
  }
  public void StrictlyNegative([StrictlyNegative] int x)
  {
    if (x >= 0)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly less than 0.");
    }
  }
  public void StrictlyPositive([StrictlyPositive] int x)
  {
    if (x <= 0)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly greater than 0.");
    }
  }
  public void LessThanInt([LessThanOrEqual(42)] int x)
  {
    if (x > 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be less than or equal to 42.");
    }
  }
  public void GreaterThanInt([GreaterThanOrEqual(42)] int x)
  {
    if (x < 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be greater than or equal to 42.");
    }
  }
  public void RangeInt([Range(10, 20)] int x)
  {
    if (x is < 10 or > 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be in the range [10, 20].");
    }
  }
  public void StrictlyGreaterThanInt([StrictlyGreaterThan(42)] int x)
  {
    if (x <= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly greater than 42.");
    }
  }
  public void StrictlyLessThanInt([StrictlyLessThan(42)] int x)
  {
    if (x >= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly less than 42.");
    }
  }
  public void StrictRangeInt([StrictRange(10.0, 20.0)] int x)
  {
    if (x is <= 10 or >= 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly in the range ]10, 20[.");
    }
  }
  public void LessThanDouble([LessThanOrEqual(42.0)] int x)
  {
    if (x > 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be less than or equal to 42.");
    }
  }
  public void GreaterThanDouble([GreaterThanOrEqual(42.0)] int x)
  {
    if (x < 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be greater than or equal to 42.");
    }
  }
  public void RangeDouble([Range(10.0, 20.0)] int x)
  {
    if (x is < 10 or > 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be in the range [10, 20].");
    }
  }
  public void StrictlyGreaterThanDouble([StrictlyGreaterThan(42.0)] int x)
  {
    if (x <= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly greater than 42.");
    }
  }
  public void StrictlyLessThanDouble([StrictlyLessThan(42.0)] int x)
  {
    if (x >= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly less than 42.");
    }
  }
  public void StrictRangeDouble([StrictRange(10.0, 20.0)] int x)
  {
    if (x is <= 10 or >= 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly in the range ]10, 20[.");
    }
  }
  public void LessThanUnsigned([LessThanOrEqual(42ul)] int x)
  {
    if (x > 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be less than or equal to 42.");
    }
  }
  public void GreaterThanUnsigned([GreaterThanOrEqual(42ul)] int x)
  {
    if (x < 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be greater than or equal to 42.");
    }
  }
  public void RangeUnsigned([Range(10ul, 20ul)] int x)
  {
    if (x is < 10 or > 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be in the range [10, 20].");
    }
  }
  public void StrictlyGreaterThanUnsigned([StrictlyGreaterThan(42ul)] int x)
  {
    if (x <= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly greater than 42.");
    }
  }
  public void StrictlyLessThanUnsigned([StrictlyLessThan(42ul)] int x)
  {
    if (x >= 42)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly less than 42.");
    }
  }
  public void StrictRangeUnsigned([StrictRange(10ul, 20ul)] int x)
  {
    if (x is <= 10 or >= 20)
    {
      throw new ArgumentOutOfRangeException("x", x, "The 'x' parameter must be strictly in the range ]10, 20[.");
    }
  }
}