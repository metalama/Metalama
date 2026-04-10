[MyAspect]
public class A
{
  public A(int x, string? z = null, [AspectGenerated] int p = 15)
  {
    this.X = x;
  }
  public int X { get; set; }
}
public class C : A
{
  public C(int x) : base(42, p: 51)
  {
    this.Y = x;
  }
  public int Y { get; }
}