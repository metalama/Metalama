[MyAspect]
public record Base
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public int X { get; init; }
  public Base(int X, [AspectGenerated] int p = 15)
  {
    this.X = X;
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
}