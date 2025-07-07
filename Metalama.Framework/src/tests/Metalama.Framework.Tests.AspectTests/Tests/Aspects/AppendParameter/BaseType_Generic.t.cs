[MyAspect]
public class A<T>
{
  public A(int x, global::System.Int32 p = 15)
  {
    this.X = x;
  }
  public int X { get; set; }
}
public class C : A<int>
{
  public C(int x) : base(42)
  {
    this.Y = x;
  }
  public int Y { get; }
}