[MyAspect]
public class C(int x, [AspectGenerated] int p = 15) : A(42)
{
  public int Y { get; } = x;
}