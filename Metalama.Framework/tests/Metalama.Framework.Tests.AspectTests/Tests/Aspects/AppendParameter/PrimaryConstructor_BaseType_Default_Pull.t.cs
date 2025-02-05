[MyAspect]
public class A(global::System.Int32 p = 15)
{
}
public class C(int x) : A(51)
{
  public int Y { get; } = x;
}