// Warning LAMA5007 on `Negative`: `The meaning of the [NegativeAttribute] attribute on C.M<T>(T)/value is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [NonPositiveAttribute] or [StrictlyNegativeAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumber_Negative;
internal class C
{
  public void M<T>([Negative] T value)
    where T : INumber<T>
  {
    if (value > T.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be less than or equal to 0.");
    }
  }
}