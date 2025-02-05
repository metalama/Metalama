[MyAspect]
public class A(global::System.Int32 p = 15)
{
}
public class C(int x) : A
{
  public int Y { get; } = x;
}