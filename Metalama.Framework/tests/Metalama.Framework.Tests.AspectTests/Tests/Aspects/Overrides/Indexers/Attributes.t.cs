[Override]
internal class TargetClass
{
  [PropertyOnly]
  public int this[[ParamOnly] int x]
  {
    [return: ReturnValueOnly]
    [method: MethodOnly]
    get
    {
      Console.WriteLine("Original Property");
      return 42;
    }
    [return: ReturnValueOnly]
    [method: MethodOnly]
    [param: ParamOnly]
    set
    {
      Console.WriteLine("Original Property");
    }
  }
}