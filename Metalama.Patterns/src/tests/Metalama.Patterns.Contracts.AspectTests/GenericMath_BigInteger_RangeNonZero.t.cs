using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_BigInteger_RangeNonZero;
internal class C
{
  public void M([Range(1, 100)] BigInteger value)
  {
    if (value < BigInteger.CreateChecked(1) || value > BigInteger.CreateChecked(100))
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be in the range [1, 100].");
    }
  }
}