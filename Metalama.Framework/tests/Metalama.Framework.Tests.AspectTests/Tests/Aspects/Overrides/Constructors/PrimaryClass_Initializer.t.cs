[Override]
public class TargetClass
{
  private int a;
  private int B { get; }
  private event EventHandler C;
  public TargetClass(int x, int y, EventHandler z)
  {
    this.a = x;
    this.B = y;
    this.C = z;
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
    global::System.Console.WriteLine($"Param z = {z}");
  }
}