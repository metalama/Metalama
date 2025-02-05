[MyAspect]
public record C(int x, global::System.Int32 p = 15) : A(42)
{
  public int Y { get; } = x;
}