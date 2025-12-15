internal class TargetClass
{
  // Method with single keyword parameter name
  public void MethodWithKeywordParam([NotNull] string @class)
  {
    if (@class == null)
    {
      throw new global::System.ArgumentNullException();
    }
    Console.WriteLine($"class = {@class}");
  }
  // Method with multiple keyword parameter names
  public int MethodWithMultipleKeywordParams([Positive] int @int, [NotNull] string @string, bool @return)
  {
    if (@int <= 0)
    {
      throw new global::System.ArgumentOutOfRangeException();
    }
    if (@string == null)
    {
      throw new global::System.ArgumentNullException();
    }
    Console.WriteLine($"int = {@int}, string = {@string}, return = {@return}");
    return @int;
  }
  // Constructor with keyword parameter names
  public TargetClass([NotNull] string @class, [Positive] int @for)
  {
    if (@class == null)
    {
      throw new global::System.ArgumentNullException();
    }
    if (@for <= 0)
    {
      throw new global::System.ArgumentOutOfRangeException();
    }
    Console.WriteLine($"class = {@class}, for = {@for}");
  }
}
