[Override]
public class TargetClass : BaseClass
{
  public TargetClass(int x, int y) : base(x)
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
  }
}