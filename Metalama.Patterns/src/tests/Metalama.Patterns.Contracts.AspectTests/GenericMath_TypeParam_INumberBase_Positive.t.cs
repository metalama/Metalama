// Warning LAMA5007 on `Positive`: `The meaning of the [PositiveAttribute] attribute on C.M<T>(T)/value is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [NonNegativeAttribute] or [StrictlyPositiveAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
// Error CS0019 on `value < T.Zero`: `Operator '<' cannot be applied to operands of type 'T' and 'T'`
using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumberBase_Positive;
internal class C
{
  public void M<T>([Positive] T value)
    where T : INumberBase<T>
  {
    if (value < T.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be greater than or equal to 0.");
    }
  }
}