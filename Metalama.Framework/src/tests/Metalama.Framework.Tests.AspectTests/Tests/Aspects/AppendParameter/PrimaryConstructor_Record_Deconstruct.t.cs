[MyAspect]
public record R(int X, int Y, [AspectGenerated] int introduced = 42)
{
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public void M()
  {
    // This deconstruction should still work after a parameter is introduced.
    var(x, y) = this;
  }
}