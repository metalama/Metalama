[MyAspect]
public record R(int X, int Y, [AspectGenerated] int introduced = 42)
{
  public void Deconstruct(out int x, out int y)
  {
    x = this.X;
    y = this.Y;
  }
  public void M()
  {
    var(a, b) = this;
  }
}