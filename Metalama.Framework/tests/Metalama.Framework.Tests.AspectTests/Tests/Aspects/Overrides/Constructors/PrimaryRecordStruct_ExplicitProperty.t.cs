[Override]
public record struct TargetStruct
{
  public int X { get; set; }
  public void Foo()
  {
    X = 42;
    var(x, y) = this;
    _ = this with
    {
      X = 13,
      Y = 42
    };
  }
  public global::System.Int32 Y { get; init; }
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public TargetStruct(int X, int Y)
  {
    this.Y = Y;
    this.X = X;
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param X = {X}");
    global::System.Console.WriteLine($"Param Y = {Y}");
  }
}