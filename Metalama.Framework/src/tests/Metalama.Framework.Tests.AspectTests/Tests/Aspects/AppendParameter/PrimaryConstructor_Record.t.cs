[MyAspect]
public record C(int x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p = 15) : A(42)
{
  public void Deconstruct(out int x)
  {
    x = this.x;
  }
  public int Y { get; } = x;
}