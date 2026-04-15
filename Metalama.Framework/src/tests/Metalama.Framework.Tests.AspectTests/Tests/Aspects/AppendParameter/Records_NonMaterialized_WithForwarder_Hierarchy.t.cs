[MyAspect]
public record Base
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public int X { get; init; }
  public Base(int X, [AspectGenerated] int p)
  {
    this.X = X;
  }
  public Base(int X) : this(X: X, p: default)
  {
  }
}
public record Derived : Base
{
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public int Y { get; init; }
  public Derived(int X, int Y, [AspectGenerated] int p) : base(X, p)
  {
    this.Y = Y;
  }
  public Derived(int X, int Y) : this(X: X, Y: Y, p: default)
  {
  }
}