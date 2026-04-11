[MyAspect]
public record R
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public int X { get; init; }
  public R(int X, [AspectGenerated] int p = 15)
  {
    this.X = X;
  }
}