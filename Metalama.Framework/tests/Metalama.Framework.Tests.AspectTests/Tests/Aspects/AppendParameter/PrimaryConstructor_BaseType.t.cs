[MyAspect]
public class A(int x, global::System.Int32 p = 15)
{
  public int X { get; set; } = x;
}
public class C(int x) : A(42)
{
  public int Y { get; } = x;
}