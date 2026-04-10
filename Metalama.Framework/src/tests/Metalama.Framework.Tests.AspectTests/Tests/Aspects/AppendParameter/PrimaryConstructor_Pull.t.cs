[MyAspect]
public class A(int x, [AspectGenerated] int p = 15)
{
  public A(short x) : this((int)x, 51)
  {
  }
  public int X { get; set; } = x;
}