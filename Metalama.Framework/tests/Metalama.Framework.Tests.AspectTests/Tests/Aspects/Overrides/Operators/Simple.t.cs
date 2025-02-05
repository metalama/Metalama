[Override]
internal class TargetClass
{
  public static TargetClass operator +(TargetClass a, TargetClass b)
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static TargetClass operator -(TargetClass a)
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  public static explicit operator TargetClass(int x)
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
}