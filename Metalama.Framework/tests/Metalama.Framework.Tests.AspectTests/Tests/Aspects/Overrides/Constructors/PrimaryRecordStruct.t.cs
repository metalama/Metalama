[Override]
public record struct TargetStruct
{
  public void Foo()
  {
    var(x, y) = this;
    _ = this with
    {
      X = 13,
      Y = 42
    };
  }
  public global::System.Int32 X { get; init; }
  public global::System.Int32 Y { get; init; }
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public TargetStruct(int X, int Y)
  {
    this.X = X;
    this.Y = Y;
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param X = {X}");
    global::System.Console.WriteLine($"Param Y = {Y}");
  }
}