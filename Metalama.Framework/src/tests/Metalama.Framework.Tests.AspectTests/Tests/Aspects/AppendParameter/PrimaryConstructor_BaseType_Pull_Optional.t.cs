[MyAspect]
public class A(int x, string? s = null, [AspectGenerated] int p = 15)
{
  public int X { get; set; } = x;
}
public class C(int x) : A(42, p: 51)
{
  public int Y { get; } = x;
}