[Override]
internal class TargetClass
{
  public static TargetClass operator +(TargetClass a, TargetClass b)
  {
    global::System.Console.WriteLine("This is the override of (TargetClass, TargetClass) -> TargetClass.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static TargetClass operator +(int a, TargetClass b)
  {
    global::System.Console.WriteLine("This is the override of (int, TargetClass) -> TargetClass.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static TargetClass operator +(TargetClass a, int b)
  {
    global::System.Console.WriteLine("This is the override of (TargetClass, int) -> TargetClass.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static explicit operator TargetClass(int x)
  {
    global::System.Console.WriteLine("This is the override of (int) -> TargetClass.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static explicit operator int (TargetClass x)
  {
    global::System.Console.WriteLine("This is the override of (TargetClass) -> int.");
    Console.WriteLine($"This is the original operator.");
    return 42;
  }
}