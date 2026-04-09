// Warning LAMA5003 on `foo`: `The [NotNullAttribute] contract is redundant because the [NotNull] contract is automatically added by a fabric.`
public class TestClass
{
  // When both [Required] and [NotNull] are on a string, [NotNull] is redundant
  // but [Required] is not (it also validates empty/whitespace).
  public void PrintString([Required][NotNull] string foo)
  {
    if (foo == null !)
    {
      throw new ArgumentNullException("foo", "The 'foo' parameter must not be null.");
    }
    if (string.IsNullOrWhiteSpace(foo))
    {
      if (foo == null !)
      {
        throw new ArgumentNullException("foo", "The 'foo' parameter is required.");
      }
      else
      {
        throw new ArgumentException("The 'foo' parameter is required.", "foo");
      }
    }
  }
}
