[MyAspect]
public class A
{
  public A(int x, [AspectGenerated] int p)
  {
    this.X = x;
  }
  public int X { get; }
}