[Override]
public class TargetClass
{
  public static int F = 42;
  public static int P { get; } = 42;
  static TargetClass()
  {
    global::System.Console.WriteLine("This is the initializer 1.");
    global::System.Console.WriteLine("This is the initializer 2.");
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine("This is the override 1.");
    Console.WriteLine($"This is the original constructor.");
  }
}