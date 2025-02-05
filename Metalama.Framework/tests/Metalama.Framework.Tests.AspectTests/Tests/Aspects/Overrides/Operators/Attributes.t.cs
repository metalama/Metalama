[Override]
internal class TargetClass
{
  [MethodOnly]
  [return: ReturnValueOnly]
  public static TargetClass operator +([ParameterOnly] TargetClass right)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    return right;
  }
  [MethodOnly]
  [return: ReturnValueOnly]
  public static explicit operator int ([ParameterOnly] TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    return 42;
  }
}