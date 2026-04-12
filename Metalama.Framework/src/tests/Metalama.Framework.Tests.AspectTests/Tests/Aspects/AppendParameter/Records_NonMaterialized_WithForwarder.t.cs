[MyAspect]
public sealed record R
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public int X { get; init; }
  public R(int X, int p)
  {
    this.X = X;
  }
  [SourceCompatibilityConstructor]
  public R(int X) : this(X: X, p: default)
  {
  }
}