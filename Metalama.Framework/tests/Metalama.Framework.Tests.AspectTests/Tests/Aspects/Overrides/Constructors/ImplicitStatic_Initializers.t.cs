[Override]
public class TargetClass
{
  public static int F = 42;
  public static int P { get; } = 42;
  public TargetClass()
  {
    global::System.Console.WriteLine($"This is the override start.");
    global::System.Console.WriteLine($"This is the override end.");
  }
}