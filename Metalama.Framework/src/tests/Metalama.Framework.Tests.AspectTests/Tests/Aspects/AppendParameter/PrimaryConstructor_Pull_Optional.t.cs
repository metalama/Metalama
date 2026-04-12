[MyAspect]
public class A(int x, string? s = null, [AspectGenerated] int p = 15)
{
  public A(short x) : this((int)x, p: 51)
  {
  }
  public int X { get; set; } = x;
}