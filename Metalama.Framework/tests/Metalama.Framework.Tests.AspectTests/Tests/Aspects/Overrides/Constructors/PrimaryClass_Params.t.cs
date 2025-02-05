[Override]
public class TargetClass
{
  public TargetClass(int x, params int[] y)
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
  }
}