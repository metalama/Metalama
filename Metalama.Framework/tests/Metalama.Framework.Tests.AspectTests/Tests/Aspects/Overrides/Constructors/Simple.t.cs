[Override]
public class TargetClass : BaseClass
{
  public TargetClass(int x, string s) : base(x)
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param s = {s}");
    Console.WriteLine($"This is the original constructor.");
  }
  public TargetClass() : this(42, "42")
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine($"This is the original constructor.");
  }
}