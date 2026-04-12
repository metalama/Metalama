[MyAspect]
public record C(int x, [AspectGenerated] int p = 15) : A(42)
{
  public void Deconstruct(out int x)
  {
    x = this.x;
  }
  public int Y { get; } = x;
}