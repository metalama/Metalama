[Override]
public class TargetClass
{
  public int Z;
  public TargetClass(int x, global::System.Int32 introduced = 42)
  {
    this.Z = x;
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("This is the override 1.");
    global::System.Console.WriteLine($"Param x = {x}");
  }
}