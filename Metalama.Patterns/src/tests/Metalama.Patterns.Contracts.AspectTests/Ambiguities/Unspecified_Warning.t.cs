// Warning LAMA5007 on `Positive`: `The meaning of the [PositiveAttribute] attribute on C.A is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [NonNegativeAttribute] or [StrictlyPositiveAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
// Warning LAMA5007 on `Negative`: `The meaning of the [NegativeAttribute] attribute on C.B is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [NonPositiveAttribute] or [StrictlyNegativeAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
// Warning LAMA5007 on `GreaterThan( 5 )`: `The meaning of the [GreaterThanAttribute] attribute on C.D is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [GreaterThanOrEqualAttribute] or [StrictlyGreaterThanAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
// Warning LAMA5007 on `LessThan( 5 )`: `The meaning of the [LessThanAttribute] attribute on C.E is ambiguous because the inequality strictness is not specified. It is now interpreted as NonStrict, which is non-standard, and this behavior might be changed in the future. Use either [LessThanOrEqualAttribute] or [StrictlyLessThanAttribute] or specify the DefaultInequalityStrictness property in ContractOptions using the ConfigureContracts fabric extension method.`
using System;
namespace Metalama.Patterns.Contracts.AspectTests.Ambiguities.Unspecified_Warning;
public class C
{
  private int _a;
  [Positive]
  public int A
  {
    get
    {
      return _a;
    }
    set
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'A' property must be greater than or equal to 0.");
      }
      _a = value;
    }
  }
  private int _b;
  [Negative]
  public int B
  {
    get
    {
      return _b;
    }
    set
    {
      if (value > 0)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'B' property must be less than or equal to 0.");
      }
      _b = value;
    }
  }
  private int _d;
  [GreaterThan(5)]
  public int D
  {
    get
    {
      return _d;
    }
    set
    {
      if (value < 5)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'D' property must be greater than or equal to 5.");
      }
      _d = value;
    }
  }
  private int _e;
  [LessThan(5)]
  public int E
  {
    get
    {
      return _e;
    }
    set
    {
      if (value > 5)
      {
        throw new ArgumentOutOfRangeException("value", value, "The 'E' property must be less than or equal to 5.");
      }
      _e = value;
    }
  }
}