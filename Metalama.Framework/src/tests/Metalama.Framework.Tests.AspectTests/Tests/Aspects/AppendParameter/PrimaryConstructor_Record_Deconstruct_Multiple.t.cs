[MyAspect]
public record R(int X, int Y, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced1 = 0, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced2 = 0)
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