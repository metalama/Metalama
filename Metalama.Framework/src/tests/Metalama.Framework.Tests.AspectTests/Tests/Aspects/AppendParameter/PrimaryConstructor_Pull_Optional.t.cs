[MyAspect]
public class A(int x, string? s = null, global::System.Int32 p = 15)
{
  public A(short x) : this((int)x, p: 51)
  {
  }
  public int X { get; set; } = x;
}