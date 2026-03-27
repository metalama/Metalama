[MyAspect]
public record R(int X, int Y, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced = 42)
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