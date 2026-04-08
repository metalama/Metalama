// Warning LAMA5007 on `Positive`: `The meaning of the [PositiveAttribute] attribute on C.M(BigInteger)/value is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [NonNegativeAttribute] or [StrictlyPositiveAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
using System;
using System.Numerics;
namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_BigInteger_Positive;
internal class C
{
  public void M([Positive] BigInteger value)
  {
    if (value < BigInteger.Zero)
    {
      throw new ArgumentOutOfRangeException("value", value, "The 'value' parameter must be greater than or equal to 0.");
    }
  }
}