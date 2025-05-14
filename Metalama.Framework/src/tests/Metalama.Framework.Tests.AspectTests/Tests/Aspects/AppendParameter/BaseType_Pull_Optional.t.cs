[MyAspect]
public class A
{
  public A(int x, string? z = null, global::System.Int32 p = 15)
  {
    X = x;
  }
  public int X { get; set; }
}
public class C : A
{
  public C(int x) : base(42, p: 51)
  {
    Y = x;
  }
  public int Y { get; }
}