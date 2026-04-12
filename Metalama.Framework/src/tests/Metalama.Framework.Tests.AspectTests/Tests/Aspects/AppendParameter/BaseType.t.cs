[MyAspect]
public class A
{
  public A(int x, [AspectGenerated] int p = 15)
  {
    X = x;
  }
  public int X { get; set; }
}
public class C : A
{
  public C(int x) : base(42)
  {
    Y = x;
  }
  public int Y { get; }
}