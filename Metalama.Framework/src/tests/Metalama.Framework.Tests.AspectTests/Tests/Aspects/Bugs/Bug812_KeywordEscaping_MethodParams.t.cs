internal class NotNullAttribute : ContractAspect
{
  public override void Validate(dynamic? value) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
internal class PositiveAttribute : ContractAspect
{
  public override void Validate(dynamic? value) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
internal class TargetClass
{
  public void MethodWithKeywordParam(string @class)
  {
    if (@class == null)
    {
      throw new ArgumentNullException();
    }
    Console.WriteLine($"class = {@class}");
  }
  public int MethodWithMultipleKeywordParams(int @int, string @string, bool @return)
  {
    if (@int <= 0)
    {
      throw new ArgumentOutOfRangeException();
    }
    if (@string == null)
    {
      throw new ArgumentNullException();
    }
    Console.WriteLine($"int = {@int}, string = {@string}, return = {@return}");
    return @int;
  }
  public TargetClass(string @class, int @for)
  {
    if (@class == null)
    {
      throw new ArgumentNullException();
    }
    if (@for <= 0)
    {
      throw new ArgumentOutOfRangeException();
    }
    Console.WriteLine($"class = {@class}, for = {@for}");
  }
}
