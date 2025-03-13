[Override]
public record class TargetClass
{
  public int Z;
  public global::System.Int32 x { get; init; }
  public global::System.Int32 introduced { get; init; }
  public void Deconstruct(out int x, out global::System.Int32 introduced)
  {
    x = this.x;
    introduced = this.introduced;
  }
  public TargetClass(int x, global::System.Int32 introduced = 42)
  {
    this.x = x;
    this.introduced = introduced;
    this.Z = x;
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("This is the override 1.");
    global::System.Console.WriteLine($"Param x = {x}");
  }
}