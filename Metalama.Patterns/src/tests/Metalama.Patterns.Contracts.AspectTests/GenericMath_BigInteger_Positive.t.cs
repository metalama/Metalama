using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_BigInteger_Positive;
internal class C
{
  public void M([NonNegative] BigInteger value)
  {
    if (value < BigInteger.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be greater than or equal to 0.");
    }
  }
}