[Override]
public class TargetClass
{
  public const int Foo = 42;
  public static int Hoo = 42;
  public static readonly int Goo = 42;
  public static int Boo { get; } = 42;
  public static event EventHandler Zoo = null !;
  public int Bar;
  public TargetClass(int x, int y)
  {
    this.Bar = x;
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
  }
}