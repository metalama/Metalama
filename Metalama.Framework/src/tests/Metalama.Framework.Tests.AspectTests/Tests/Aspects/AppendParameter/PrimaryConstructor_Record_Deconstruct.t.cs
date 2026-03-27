[MyAspect]
public record R(int X, int Y, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced = 42)
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