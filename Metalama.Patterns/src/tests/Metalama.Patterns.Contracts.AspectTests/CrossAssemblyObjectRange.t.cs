using System;
using Metalama.Patterns.Contracts.Numeric;
namespace Metalama.Patterns.Contracts.AspectTests.CrossAssemblyObjectRange;
internal class C : IValidated
{
  public object M(object a, object b, out object c)
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
    if (NumberComparer.IsStrictlyGreaterThan(c, 100) == true)
    {
      throw new PostconditionViolationException("The 'c' parameter must be less than or equal to 100.", c);
    }
    if (NumberComparer.IsGreaterThan(returnValue, 0) == true)
    {
      throw new PostconditionViolationException("The return value must be strictly greater than 0.", returnValue);
    }
    return returnValue;
  }
}