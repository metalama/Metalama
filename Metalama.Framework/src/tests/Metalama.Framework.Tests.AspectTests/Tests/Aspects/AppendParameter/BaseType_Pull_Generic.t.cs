[MyAspect]
public class A<T>
{
  public A(int x, [AspectGenerated] int p = 15)
  {
    this.X = x;
  }
  public int X { get; set; }
}
public class C : A<string>
{
  public C(int x) : base(42, 51)
  {
    this.Y = x;
  }
  public int Y { get; }
}