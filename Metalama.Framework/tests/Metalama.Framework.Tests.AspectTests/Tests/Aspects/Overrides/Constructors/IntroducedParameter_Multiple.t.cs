[Override1]
[Override2]
public class TargetClass
{
  public TargetClass(global::System.Int32 introduced = 42)
  {
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine($"Param introduced = {introduced}");
    global::System.Console.WriteLine("This is the override 1.");
    Console.WriteLine($"This is the original constructor.");
  }
  public TargetClass(int x, global::System.Int32 introduced = 42)
  {
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param introduced = {introduced}");
    global::System.Console.WriteLine("This is the override 1.");
    global::System.Console.WriteLine($"Param x = {x}");
    Console.WriteLine($"This is the original constructor.");
  }
}