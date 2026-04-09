using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_IBinaryInteger_RangeNonZero;
internal class C
{
  public void M<T>([Range(1, 255)] T value)
    where T : IBinaryInteger<T>
  {
    if (value < T.CreateChecked(1) || value > T.CreateChecked(255))
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be in the range [1, 255].");
    }
  }
}