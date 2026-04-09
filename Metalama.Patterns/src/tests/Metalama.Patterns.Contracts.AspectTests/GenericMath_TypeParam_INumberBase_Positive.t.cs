// Error CS0019 on `value < T.Zero`: `Operator '<' cannot be applied to operands of type 'T' and 'T'`
using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumberBase_Positive;
internal class C
{
  public void M<T>([NonNegative] T value)
    where T : INumberBase<T>
  {
    if (value < T.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be greater than or equal to 0.");
    }
  }
}