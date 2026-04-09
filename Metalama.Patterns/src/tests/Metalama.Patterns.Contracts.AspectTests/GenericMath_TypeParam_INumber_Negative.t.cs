using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumber_Negative;
internal class C
{
  public void M<T>([NonPositive] T value)
    where T : INumber<T>
  {
    if (value > T.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be less than or equal to 0.");
    }
  }
}