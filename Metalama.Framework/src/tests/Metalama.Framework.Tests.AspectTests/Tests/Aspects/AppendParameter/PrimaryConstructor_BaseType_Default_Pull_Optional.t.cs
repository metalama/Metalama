[MyAspect]
public class A(string? x = null, [AspectGenerated] int p = 15)
{
}
public class C(int x) : A(p: 51)
{
  public int Y { get; } = x;
}