[Override]
public record class TargetClass
{
  public void Deconstruct(out int x)
  {
    x = this.x;
  }
  public int Z;
  public global::System.Int32 x { get; init; }
  public TargetClass(int x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced = 42)
  {
    this.x = x;
    this.Z = x;
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("This is the override 1.");
    global::System.Console.WriteLine($"Param x = {x}");
  }
}