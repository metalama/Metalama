[Override]
public partial class TargetClass : B
{
}
public partial class TargetClass
{
  public TargetClass(int x, int y)
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
  }
}
public partial class TargetClass : I
{
}