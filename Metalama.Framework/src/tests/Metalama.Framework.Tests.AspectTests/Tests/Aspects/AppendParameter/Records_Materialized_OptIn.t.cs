[MyAspect]
public record R(int X, [AspectGenerated] int p = 15)
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
}