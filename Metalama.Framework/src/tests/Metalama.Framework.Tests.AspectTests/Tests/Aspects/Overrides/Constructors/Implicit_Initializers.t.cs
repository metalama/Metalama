[Override]
public class TargetClass
{
  public int F = 42;
  public int P { get; } = 42;
  public TargetClass()
  {
    global::System.Console.WriteLine($"This is the override start.");
    global::System.Console.WriteLine($"This is the override end.");
  }
}