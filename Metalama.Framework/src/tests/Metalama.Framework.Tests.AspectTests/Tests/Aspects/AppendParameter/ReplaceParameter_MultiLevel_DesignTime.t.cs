[MyAspect]
public partial class Base
{
  public Base(int x)
  {
    this.X = x;
  }
  public int X { get; }
}
public partial class Middle : Base
{
  public Middle(int x) : base(x)
  {
  }
}
public partial class Derived : Middle
{
  public Derived(int x) : base(x)
  {
  }
}
