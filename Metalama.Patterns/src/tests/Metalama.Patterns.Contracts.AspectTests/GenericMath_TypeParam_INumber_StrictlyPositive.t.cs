using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumber_StrictlyPositive;
internal class C
{
  public void M<T>([StrictlyPositive] T value)
    where T : INumber<T>
  {
    if (value <= T.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be strictly greater than 0.");
    }
  }
}