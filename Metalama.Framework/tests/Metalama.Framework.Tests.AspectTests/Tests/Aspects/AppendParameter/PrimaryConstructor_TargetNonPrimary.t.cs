[MyAspect]
public class C(int x) : A(42)
{
  public int X { get; } = x;
  public C(int x, int y, global::System.Int32 p = 15) : this(x)
  {
  }
}