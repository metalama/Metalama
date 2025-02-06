using System;
using Metalama.Patterns.Contracts.Numeric;
namespace Metalama.Patterns.Contracts.AspectTests.ObjectRange;
internal class C
{
  [return: StrictlyPositive]
  public object M([NonNegative] object a, [Range(0, 100)] object b, [LessThanOrEqual(101, decimalPlaces: 2)] out object c)
  {
    if (NumberComparer.IsStrictlySmallerThan(a, 0) == true)
    {
      throw new ArgumentOutOfRangeException("a", a, "The 'a' parameter must be greater than or equal to 0.");
    }
    if (NumberComparer.IsStrictlySmallerThan(b, 0) == true || NumberComparer.IsStrictlyGreaterThan(b, 100) == true)
    {
      throw new ArgumentOutOfRangeException("b", b, "The 'b' parameter must be in the range [0, 100].");
    }
    object returnValue;
    c = a;
    returnValue = b;
    if (NumberComparer.IsStrictlyGreaterThan(c, 1.01M) == true)
    {
      throw new PostconditionViolationException("The 'c' parameter must be less than or equal to 1.01.", c);
    }
    if (NumberComparer.IsGreaterThan(returnValue, 0) == true)
    {
      throw new PostconditionViolationException("The return value must be strictly greater than 0.", returnValue);
    }
    return returnValue;
  }
}