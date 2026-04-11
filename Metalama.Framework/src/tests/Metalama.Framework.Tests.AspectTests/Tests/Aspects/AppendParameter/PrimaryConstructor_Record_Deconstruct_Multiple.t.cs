[MyAspect]
public record R(int X, int Y, [AspectGenerated] int introduced1 = 0, [AspectGenerated] int introduced2 = 0)
{
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public void M()
  {
    var(x, y) = this;
  }
}