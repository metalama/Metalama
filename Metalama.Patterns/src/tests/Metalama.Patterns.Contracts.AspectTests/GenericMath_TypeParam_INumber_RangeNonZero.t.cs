using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumber_RangeNonZero;
internal class C
{
  public void M<T>([Range(1, 100)] T value)
    where T : INumber<T>
  {
    if (value < T.CreateChecked(1) || value > T.CreateChecked(100))
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be in the range [1, 100].");
    }
  }
}