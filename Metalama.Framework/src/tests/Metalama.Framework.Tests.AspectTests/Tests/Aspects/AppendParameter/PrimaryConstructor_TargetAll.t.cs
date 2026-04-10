[MyAspect]
public class C(int x, [AspectGenerated] int p = 15) : A(42)
{
  public int X { get; } = x;
  public C(int x, int y, [AspectGenerated] int p = 15) : this(x)
  {
  }
}